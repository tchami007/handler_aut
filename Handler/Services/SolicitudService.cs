using Handler.Controllers.Dtos;
using Handler.Infrastructure;
using Handler.Models;

namespace Handler.Services
{
    /// <summary>
    /// Servicio encargado de la lógica de registro de solicitudes de movimiento sobre cuentas.
    /// Controla idempotencia, tipo de movimiento, saldo suficiente y publica el resultado en RabbitMQ.
    /// </summary>
    public class SolicitudService : ISolicitudService
    {
    private readonly HandlerDbContext _db;
    private readonly RabbitMqPublisher _publisher;
    private readonly IRabbitConfigService _configService;
    private readonly IHandlerStatusService _statusService;

    /// <summary>
    /// Constructor con inyección de dependencias.
    /// </summary>
        public SolicitudService(HandlerDbContext db, RabbitMqPublisher publisher, IRabbitConfigService configService, IHandlerStatusService statusService)
        {
            _db = db;
            _publisher = publisher;
            _configService = configService;
            _statusService = statusService;
        }

        private int CalcularCola(long numeroCuenta)
        /// <summary>
        /// Calcula el número de cola destino para el mensaje RabbitMQ según el número de cuenta.
        /// </summary>
        {
            var config = _configService.GetConfig();
            int cantidadColas = config.Colas?.Count ?? 1;
            // Ajuste: las colas van de cola_1 a cola_N
            return (int)(numeroCuenta % cantidadColas) + 1;
        }

        public SolicitudResultadoDto RegistrarSolicitudConSaldo(RegistroSolicitudDto dto)
        /// <summary>
        /// Registra una solicitud de movimiento sobre una cuenta.
        /// Aplica validaciones de negocio y publica el resultado en la cola correspondiente.
        /// </summary>
        /// <param name="dto">Datos de la solicitud a registrar</param>
        /// <returns>DTO con el resultado de la operación, saldo y estado</returns>
        {
            // Validar estado del handler
            if (!_statusService.EstaActivo())
            {
                return new SolicitudResultadoDto { Id = 0, Saldo = 0, Status = 99 };
            }
            int status = 0;
            decimal saldoFinal = 0;
            // Crear la solicitud
            var solicitud = new SolicitudDebito
            {
                FechaSolicitud = DateTime.UtcNow.Date,
                FechaReal = DateTime.UtcNow,
                TipoMovimiento = dto.TipoMovimiento,
                MovimientoOriginalId = dto.MovimientoOriginalId,
                NumeroComprobante = dto.NumeroComprobante,
                Monto = dto.Monto
            };
            // Buscar la cuenta
            var cuenta = _db.Cuentas.FirstOrDefault(c => c.Numero == dto.NumeroCuenta);
            // Validar la cuenta
            if (cuenta == null) // La cuenta no existe
            {
                solicitud.CuentaId = 0;
                solicitud.Estado = "rechazada";
                status = 1;
                saldoFinal = 0;
            }
            else // La cuenta existe
            {
                solicitud.CuentaId = cuenta.Id;
                saldoFinal = cuenta.Saldo;

                // Control de idempotencia
                var existe = _db.SolicitudesDebito.Any(s =>
                    s.CuentaId == cuenta.Id &&
                    s.Monto == dto.Monto &&
                    s.NumeroComprobante == dto.NumeroComprobante &&
                    s.FechaSolicitud.Date == DateTime.UtcNow.Date &&
                    s.Estado == "autorizada");
                if (existe) // Solicitud idéntica ya autorizada hoy
                {
                    solicitud.Estado = "rechazada";
                    status = 2;
                }
                // Validar tipo de movimiento
                else if (dto.TipoMovimiento != "debito" && dto.TipoMovimiento != "credito" && dto.TipoMovimiento != "contrasiento_debito" && dto.TipoMovimiento != "contrasiento_credito")
                {
                    solicitud.Estado = "rechazada";
                    status = 3;
                }
                // Validar saldo suficiente para débitos y contrasientos de crédito
                else if ((dto.TipoMovimiento == "debito" || dto.TipoMovimiento == "contrasiento_credito") && cuenta.Saldo < dto.Monto)
                {
                    solicitud.Estado = "rechazada";
                    status = 4;
                }
                // Si pasa todas las validaciones, autorizar y actualizar saldo
                else
                {
                    solicitud.Estado = "autorizada";
                    switch (dto.TipoMovimiento)
                    {
                        case "debito":
                        case "contrasiento_credito":
                            cuenta.Saldo -= dto.Monto;
                            break;
                        case "credito":
                        case "contrasiento_debito":
                            cuenta.Saldo += dto.Monto;
                            break;
                    }
                    // Actualizar el saldo final
                    saldoFinal = cuenta.Saldo;
                    // Guardar el nuevo saldo en la base de datos  
                    _db.Cuentas.Update(cuenta);
                }
            }

            // Asignar el saldo de respuesta y el código de estado en la entidad antes de guardar
            solicitud.SaldoRespuesta = saldoFinal;
            solicitud.CodigoEstado = status;
            _db.SolicitudesDebito.Add(solicitud);
            _db.SaveChanges();

            Console.WriteLine($"[Solicitud] Registrada solicitud Id={solicitud.Id}, cuenta={dto.NumeroCuenta}, monto={dto.Monto}, tipo={dto.TipoMovimiento}, comprobante={dto.NumeroComprobante}, estado={solicitud.Estado}, saldoRespuesta={solicitud.SaldoRespuesta}");

            // Calcular la cola destino
            int colaDestino = CalcularCola(dto.NumeroCuenta);
            string nombreCola = $"cola_{colaDestino}";
            Console.WriteLine($"[RabbitMQ] Enviando mensaje a la cola: {nombreCola}");

            // Usar el nuevo DTO para el mensaje a RabbitMQ
            var mensajeDto = new SolicitudRabbitDto {
                Id = solicitud.Id,
                TipoMovimiento = dto.TipoMovimiento,
                Importe = dto.Monto,
                NumeroCuenta = dto.NumeroCuenta,
                FechaMovimiento = DateTime.UtcNow,
                NumeroComprobante = dto.NumeroComprobante,
                Contrasiento = null,
                ConnectionStringBanksys = "<CONNECTION_STRING>"
            };
            var mensaje = System.Text.Json.JsonSerializer.Serialize(mensajeDto);
            _publisher.Publish(mensaje, nombreCola);
            Console.WriteLine($"[RabbitMQ] Mensaje publicado: {mensaje}");

            return new SolicitudResultadoDto { Id = solicitud.Id, Saldo = solicitud.SaldoRespuesta, Status = status };
        }
        /// <summary>
        /// Recupera todas las solicitudes procesadas, ordenadas por fecha real de procesamiento descendente.
        /// </summary>
        public List<Handler.Controllers.Dtos.SolicitudDebitoDto> GetSolicitudesProcesadas()
        {
            return _db.SolicitudesDebito
                .OrderByDescending(s => s.FechaReal)
                .Select(s => new Handler.Controllers.Dtos.SolicitudDebitoDto
                {
                    Id = s.Id,
                    CuentaId = s.CuentaId,
                    Monto = s.Monto,
                    FechaSolicitud = s.FechaSolicitud,
                    FechaReal = s.FechaReal,
                    Estado = s.Estado,
                    CodigoEstado = s.CodigoEstado,
                    TipoMovimiento = s.TipoMovimiento,
                    MovimientoOriginalId = s.MovimientoOriginalId,
                    NumeroComprobante = s.NumeroComprobante,
                    SaldoRespuesta = s.SaldoRespuesta
                })
                .ToList();
        }
    }
}
