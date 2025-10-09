
using Handler.Controllers.Dtos;
using Handler.Infrastructure;
using Handler.Models;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace Handler.Services
{
    /// <summary>
    /// Servicio encargado de la lógica de registro de solicitudes de movimiento sobre cuentas.
    /// Controla idempotencia, tipo de movimiento, saldo suficiente y publica el resultado en RabbitMQ.
    /// </summary>
    public class SolicitudService : ISolicitudService
    {
    private readonly ICuentaRepository _cuentaRepository;
    private readonly ISolicitudRepository _solicitudRepository;
    private readonly HandlerDbContext _db; // Para transacciones serializables
    private readonly RabbitMqPublisher _publisher;
    private readonly IRabbitConfigService _configService;
    private readonly IHandlerStatusService _statusService;
    private readonly ILogger<SolicitudService> _logger;

    // Configuración de reintentos y espera
    private readonly int cantidadReintentos = 10;
    private readonly int tiempoMinimoEsperaMs = 50;
    private readonly int tiempoMaximoEsperaMs = 100;

    /// <summary>
    /// Constructor con inyección de dependencias.
    /// Nota: Mantiene DbContext para transacciones serializables complejas.
    /// </summary>
        public SolicitudService(ICuentaRepository cuentaRepository, ISolicitudRepository solicitudRepository, HandlerDbContext db, RabbitMqPublisher publisher, IRabbitConfigService configService, IHandlerStatusService statusService, ILogger<SolicitudService> logger)
        {
            _cuentaRepository = cuentaRepository;
            _solicitudRepository = solicitudRepository;
            _db = db;
            _publisher = publisher;
            _configService = configService;
            _statusService = statusService;
            _logger = logger;
        }

        private int CalcularCola(long numeroCuenta)
        /// <summary>
        /// Calcula el número de cola destino para el mensaje RabbitMQ según el número de cuenta.
        /// </summary>
        {
            var config = _configService.GetConfig();
            int cantidadColas = config.Colas?.Count ?? 0;
            
            // Si no hay colas configuradas, usar cola por defecto
            if (cantidadColas == 0)
            {
                return 1;
            }
            
            int resultadoModulo = (int)(numeroCuenta % cantidadColas);
            int colaDestino = resultadoModulo + 1;
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
            // Implementación con reintentos para manejar conflictos de concurrencia
            int reintentos = cantidadReintentos;
            
            while (reintentos-- > 0)
            {
                try
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
                    
                    _db.SaveChanges();

                    // Calcular la cola destino
                    int colaDestino = CalcularCola(dto.NumeroCuenta);
                    string nombreCola = $"cola_{colaDestino}";

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
                    try
                    {
                        _publisher.Publish(mensaje, nombreCola);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "No se pudo publicar mensaje en RabbitMQ");
                        // Continúa sin fallar - RabbitMQ es opcional para el registro
                    }

                    return new SolicitudResultadoDto { Id = solicitud.Id, Saldo = solicitud.SaldoRespuesta, Status = status, Cola = nombreCola };
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
                {
                    // Si ocurre un conflicto de concurrencia, se reintenta
                    _logger.LogWarning(ex, "Conflicto detectado al actualizar cuenta {NumeroCuenta}. Reintentando... ({Reintentos} restantes)", dto.NumeroCuenta, reintentos);
                    if (reintentos == 0)
                    {
                        // Documentar el error y devolver rechazo
                        _logger.LogError("No se pudo resolver el conflicto de concurrencia tras varios intentos para cuenta {NumeroCuenta}", dto.NumeroCuenta);
                        return new SolicitudResultadoDto { Id = 0, Saldo = 0, Status = 98 };
                    }
                    // Esperar un pequeño tiempo aleatorio para evitar colisiones repetidas
                    System.Threading.Thread.Sleep(new Random().Next(tiempoMinimoEsperaMs, tiempoMaximoEsperaMs));
                    continue;
                }
                catch (Exception ex) when (ex.Message.Contains("deadlock") || ex.Message.Contains("timeout") || ex.Message.Contains("lock"))
                {
                    // Manejo específico para deadlocks, timeouts y problemas de bloqueo
                    _logger.LogWarning(ex, "Problema de bloqueo detectado para cuenta {NumeroCuenta}. Reintentando... ({Reintentos} restantes)", dto.NumeroCuenta, reintentos);
                    if (reintentos == 0)
                    {
                        _logger.LogError("No se pudo completar la operación para cuenta {NumeroCuenta} después de {CantidadReintentos} reintentos por bloqueo", dto.NumeroCuenta, cantidadReintentos);
                        return new SolicitudResultadoDto { Id = 0, Saldo = 0, Status = 97 };
                    }
                    // Esperar un pequeño tiempo aleatorio para evitar colisiones repetidas
                    System.Threading.Thread.Sleep(new Random().Next(tiempoMinimoEsperaMs, tiempoMaximoEsperaMs));
                    continue;
                }
                catch (Exception ex)
                {
                    // Manejo de errores críticos no recuperables
                    _logger.LogError(ex, "Error crítico en RegistrarSolicitudConSaldo para cuenta {NumeroCuenta}", dto.NumeroCuenta);
                    return new SolicitudResultadoDto { Id = 0, Saldo = 0, Status = 96 };
                }
            }
            // Si llega aquí, se agotaron los reintentos
            return new SolicitudResultadoDto { Id = 0, Saldo = 0, Status = 97 };
        }
        /// <summary>
        /// Recupera todas las solicitudes procesadas, ordenadas por fecha real de procesamiento descendente.
        /// </summary>
        public List<Handler.Controllers.Dtos.SolicitudDebitoDto> GetSolicitudesProcesadas()
        {
            var solicitudes = _solicitudRepository.GetAllAsync().Result;
            return solicitudes.Select(s => new Handler.Controllers.Dtos.SolicitudDebitoDto
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
            }).ToList();
        }
        
        /// <summary>
        /// Recupera todas las solicitudes procesadas para una cuenta específica, ordenadas por fecha real ascendente.
        /// </summary>
        public List<Handler.Controllers.Dtos.SolicitudDebitoDto> GetSolicitudesPorCuenta(long numeroCuenta)
        {
            var cuenta = _cuentaRepository.GetByNumeroAsync(numeroCuenta).Result;
            if (cuenta == null)
                return new List<Handler.Controllers.Dtos.SolicitudDebitoDto>();
                
            var solicitudes = _solicitudRepository.GetByCuentaIdAsync(cuenta.Id).Result;
            return solicitudes.Select(s => new Handler.Controllers.Dtos.SolicitudDebitoDto
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
            }).ToList();
        }
    }
}
