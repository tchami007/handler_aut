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
    private readonly bool seguimientoHabilitado = true; // Cambia a 'false' para ocultar logs de seguimiento

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
            int resultadoModulo = (int)(numeroCuenta % cantidadColas);
            int colaDestino = resultadoModulo + 1;
            if (seguimientoHabilitado)
            {
                Console.WriteLine($"[Seguimiento][CalcularCola] numeroCuenta={numeroCuenta}, cantidadColas={cantidadColas}, resultadoModulo={resultadoModulo}, colaDestino={colaDestino}");
            }
            // Ajuste: las colas van de cola_1 a cola_N
            return colaDestino;
        }

        public SolicitudResultadoDto RegistrarSolicitudConSaldo(RegistroSolicitudDto dto)
        /// <summary>
        /// Registra una solicitud de movimiento sobre una cuenta.
        /// Aplica validaciones de negocio y publica el resultado en la cola correspondiente.
        /// </summary>
        /// <param name="dto">Datos de la solicitud a registrar</param>
        /// <returns>DTO con el resultado de la operación, saldo y estado</returns>
        {
            // Implementación de control de concurrencia optimista usando RowVersion
            // Si ocurre un conflicto de concurrencia, se reintenta hasta 10 veces

            int reintentos = 10;
            
            while (reintentos-- > 0)
            {
                using (var transaction = _db.Database.BeginTransaction())
                {
                    // Validar estado del handler
                    if (!_statusService.EstaActivo())
                    {
                        return new SolicitudResultadoDto { Id = 0, Saldo = 0, Status = 99 };
                    }
                    int status = 0;
                    decimal saldoFinal = 0;
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
                    if (cuenta == null)
                    {
                        solicitud.CuentaId = 0;
                        solicitud.Estado = "rechazada";
                        status = 1;
                        saldoFinal = 0;
                    }
                    else
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
                        if (existe)
                        {
                            solicitud.Estado = "rechazada";
                            status = 2;
                        }
                        else if (dto.TipoMovimiento != "debito" && dto.TipoMovimiento != "credito" && dto.TipoMovimiento != "contrasiento_debito" && dto.TipoMovimiento != "contrasiento_credito")
                        {
                            solicitud.Estado = "rechazada";
                            status = 3;
                        }
                        else if ((dto.TipoMovimiento == "debito" || dto.TipoMovimiento == "contrasiento_credito") && cuenta.Saldo < dto.Monto)
                        {
                            solicitud.Estado = "rechazada";
                            status = 4;
                        }
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
                            saldoFinal = cuenta.Saldo;
                            // Guardar el nuevo saldo en la base de datos
                            _db.Cuentas.Update(cuenta);
                        }
                    }

                    solicitud.SaldoRespuesta = saldoFinal;
                    solicitud.CodigoEstado = status;
                    _db.SolicitudesDebito.Add(solicitud);
                    try
                    {
                        _db.SaveChanges();
                        // Si SaveChanges fue exitoso, no hubo conflicto de concurrencia
                        Console.WriteLine($"[Solicitud] Registrada solicitud Id={solicitud.Id}, cuenta={dto.NumeroCuenta}, monto={dto.Monto}, tipo={dto.TipoMovimiento}, comprobante={dto.NumeroComprobante}, estado={solicitud.Estado}, saldoRespuesta={solicitud.SaldoRespuesta}");

                        // Calcular la cola destino
                        int colaDestino = CalcularCola(dto.NumeroCuenta);
                        string nombreCola = $"cola_{colaDestino}";
                        if (seguimientoHabilitado)
                        {
                            Console.WriteLine($"[Seguimiento][RegistrarSolicitudConSaldo] Asignando solicitud Id={solicitud.Id} a la cola: {nombreCola} (cuenta={dto.NumeroCuenta})");
                        }
                        Console.WriteLine($"[RabbitMQ] Enviando mensaje a la cola: {nombreCola}");

                        // Usar el nuevo DTO para el mensaje a RabbitMQ
                        var mensajeDto = new SolicitudRabbitDto
                        {
                            Id = solicitud.Id,
                            TipoMovimiento = dto.TipoMovimiento,
                            Importe = dto.Monto,
                            NumeroCuenta = dto.NumeroCuenta,
                            FechaMovimiento = DateTime.UtcNow,
                            NumeroComprobante = dto.NumeroComprobante,
                            Contrasiento = null,
                            ConnectionStringBanksys = "<CONNECTION_STRING>"
                        };

                        // Serializar el mensaje
                        var mensaje = System.Text.Json.JsonSerializer.Serialize(mensajeDto);

                        // Publicar el mensaje en la cola correspondiente
                        _publisher.Publish(mensaje, nombreCola);
                        Console.WriteLine($"[RabbitMQ] Mensaje publicado: {mensaje}");

                        // Confirmar la transacción
                        transaction.Commit();

                        return new SolicitudResultadoDto { Id = solicitud.Id, Saldo = solicitud.SaldoRespuesta, Status = status, Cola = nombreCola };
                    }
                    catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
                    {
                        // Si ocurre un conflicto de concurrencia, se reintenta
                        Console.WriteLine($"[Concurrencia] Conflicto detectado al actualizar cuenta {dto.NumeroCuenta}. Reintentando... ({reintentos} restantes)");
                        transaction.Rollback();
                        if (reintentos == 0)
                        {
                            // Documentar el error y devolver rechazo
                            Console.WriteLine($"[Concurrencia] No se pudo resolver el conflicto de concurrencia tras varios intentos para cuenta {dto.NumeroCuenta}.");
                            return new SolicitudResultadoDto { Id = 0, Saldo = 0, Status = 98 };
                        }
                        // Esperar un pequeño tiempo aleatorio para evitar colisiones repetidas
                        System.Threading.Thread.Sleep(new Random().Next(50, 100));
                        continue;
                    }
                }
            }
            // Si llega aquí, algo falló
            return new SolicitudResultadoDto { Id = 0, Saldo = 0, Status = 97 };
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
        /// <summary>
        /// Recupera todas las solicitudes procesadas para una cuenta específica, ordenadas por fecha real ascendente.
        /// </summary>
        public List<Handler.Controllers.Dtos.SolicitudDebitoDto> GetSolicitudesPorCuenta(long numeroCuenta)
        {
            var cuenta = _db.Cuentas.FirstOrDefault(c => c.Numero == numeroCuenta);
            if (cuenta == null)
                return new List<Handler.Controllers.Dtos.SolicitudDebitoDto>();
            return _db.SolicitudesDebito
                .Where(s => s.CuentaId == cuenta.Id)
                .OrderBy(s => s.FechaReal)
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
