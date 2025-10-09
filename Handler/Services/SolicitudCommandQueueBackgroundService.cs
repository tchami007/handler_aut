using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks;
using Handler.Controllers.Dtos;
using Handler.Infrastructure;
using Handler.Models;
using Handler.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Handler.Services
{
    /// <summary>
    /// Servicio de cola de comandos que valida rápidamente y actualiza saldo en background.
    /// Respuesta inmediata con saldo actual, actualización diferida en cola.
    /// </summary>
    public class SolicitudCommandQueueBackgroundService : ISolicitudCommandQueueService
    {
        private readonly ConcurrentDictionary<int, Channel<RegistroSolicitudDto>> _colas;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SolicitudCommandQueueBackgroundService> _logger;
        private readonly int _cantidadColas;

        // Configuración de reintentos y espera
        private readonly int cantidadReintentos = 10;
        private readonly int tiempoMinimoEsperaMs = 50;
        private readonly int tiempoMaximoEsperaMs = 100;

        /// <summary>
        /// Constructor que inicializa las colas y los workers.
        /// NOTA: Este servicio requiere IServiceProvider para crear scopes dinámicos
        /// en el procesamiento asíncrono de múltiples colas concurrentes.
        /// </summary>
        public SolicitudCommandQueueBackgroundService(IServiceProvider serviceProvider, ILogger<SolicitudCommandQueueBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _colas = new ConcurrentDictionary<int, Channel<RegistroSolicitudDto>>();

            // Obtener cantidad de colas desde config
            using (var scope = _serviceProvider.CreateScope())
            {
                var configService = scope.ServiceProvider.GetRequiredService<IRabbitConfigService>();
                _cantidadColas = configService.GetConfig().Colas?.Count ?? 1;
            }

            // Inicializar canales y workers
            for (int i = 1; i <= _cantidadColas; i++)
            {
                var channel = Channel.CreateUnbounded<RegistroSolicitudDto>();
                _colas[i] = channel;
                _ = Task.Run(() => ProcesarColaAsync(i, channel));
            }
        }

        /// <summary>
        /// Evalúa el saldo y encola la solicitud solo si es válida. Retorna resultado inmediato.
        /// El saldo se actualiza en background.
        /// 
        /// NOTA: Usa Service Locator pattern por necesidad arquitectónica:
        /// - Respuesta inmediata sin bloquear con transacciones serializables
        /// - Manejo de múltiples colas concurrentes
        /// - Inyección directa no es viable en este contexto asíncrono
        /// </summary>
        public SolicitudResultadoDto EncolarSolicitud(RegistroSolicitudDto dto)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HandlerDbContext>();
            var statusService = scope.ServiceProvider.GetRequiredService<IHandlerStatusService>();

            if (!statusService.EstaActivo())
            {
                // Migrado: Usando SolicitudResultadoDtoFactory
                return Handler.Shared.SolicitudResultadoDtoFactory.CrearServicioInactivo();
            }

            // Migrado: Crear servicio de validación
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var validationLogger = loggerFactory.CreateLogger<Handler.Shared.SolicitudValidationService>();
            var validationService = new Handler.Shared.SolicitudValidationService(validationLogger);

            int reintentos = cantidadReintentos;
            while (reintentos-- > 0)
            {
                try
                {
                    // Migrado: Usar servicio de validación en lugar de validaciones manuales
                    var validationResult = validationService.ValidarSolicitud(dto, db);
                    
                    // Migrado: Usar servicio de distribución de colas
                    var colaDistService = CrearColaDistributionService();
                    string nombreCola = colaDistService.CalcularNombreCola(dto.NumeroCuenta);
                    
                    if (!validationResult.IsValid)
                    {
                        return Handler.Shared.SolicitudResultadoDtoFactory.CrearDesdeValidacion(validationResult, nombreCola);
                    }

                    var cuenta = validationResult.Cuenta!;
                    decimal saldoFinal = cuenta.Saldo;

                    // Si pasa todas las validaciones, encola y responde como aceptado
                    int particion = colaDistService.CalcularCola(dto.NumeroCuenta);
                    _colas[particion].Writer.TryWrite(dto);
                    
                    // No se conoce el Id aún, pero se puede devolver status 0 (aceptado) y saldo actual
                    // Migrado: Usando SolicitudResultadoDtoFactory
                    return Handler.Shared.SolicitudResultadoDtoFactory.CrearAutorizada(0, saldoFinal, nombreCola);
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
                {
                    if (reintentos == 0)
                    {
                        _logger.LogError(ex, "No se pudo resolver el conflicto para cuenta {NumeroCuenta} después de {CantidadReintentos} reintentos", 
                            dto.NumeroCuenta, cantidadReintentos);
                        // Migrado: Usando SolicitudResultadoDtoFactory
                        return Handler.Shared.SolicitudResultadoDtoFactory.CrearErrorConcurrencia();
                    }
                    _logger.LogWarning(ex, "Conflicto detectado para cuenta {NumeroCuenta}. Reintentando... ({Reintentos} restantes)", dto.NumeroCuenta, reintentos);
                    Thread.Sleep(new Random().Next(tiempoMinimoEsperaMs, tiempoMaximoEsperaMs));
                    continue;
                }
                catch (Exception ex) when (ex.Message.Contains("deadlock") || ex.Message.Contains("timeout") || ex.Message.Contains("lock"))
                {
                    if (reintentos == 0)
                    {
                        _logger.LogError(ex, "No se pudo completar la operación para cuenta {NumeroCuenta} después de {CantidadReintentos} reintentos por bloqueo", 
                            dto.NumeroCuenta, cantidadReintentos);
                        // Migrado: Usando SolicitudResultadoDtoFactory
                        return Handler.Shared.SolicitudResultadoDtoFactory.CrearErrorBloqueo();
                    }
                    _logger.LogWarning(ex, "Problema de bloqueo detectado para cuenta {NumeroCuenta}. Reintentando... ({Reintentos} restantes)", dto.NumeroCuenta, reintentos);
                    Thread.Sleep(new Random().Next(tiempoMinimoEsperaMs, tiempoMaximoEsperaMs));
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error crítico en EncolarSolicitud para cuenta {NumeroCuenta}", dto.NumeroCuenta);
                    // Migrado: Usando SolicitudResultadoDtoFactory
                    return Handler.Shared.SolicitudResultadoDtoFactory.CrearErrorCritico();
                }
            }

            // Si llegamos aquí, se agotaron todos los reintentos
            _logger.LogError("Se agotaron todos los reintentos para cuenta {NumeroCuenta}", dto.NumeroCuenta);
            // Migrado: Usando SolicitudResultadoDtoFactory
            return Handler.Shared.SolicitudResultadoDtoFactory.CrearErrorCritico();
        }

        /// <summary>
        /// Crea una instancia del servicio de distribución de colas.
        /// Migrado: Reemplaza el método CalcularCola manual por servicio compartido.
        /// </summary>
        private Handler.Shared.ColaDistributionService CrearColaDistributionService()
        {
            using var scope = _serviceProvider.CreateScope();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<Handler.Shared.ColaDistributionService>();
            return new Handler.Shared.ColaDistributionService(_cantidadColas, logger);
        }

        /// <summary>
        /// Procesa la cola de una partición en background, serializando las operaciones.
        /// Actualiza saldo y registra solicitud completa.
        /// </summary>
        private async Task ProcesarColaAsync(int particion, Channel<RegistroSolicitudDto> channel)
        {
            await foreach (var dto in channel.Reader.ReadAllAsync())
            {
                try
                {
                    await ProcesarSolicitudConActualizacionSaldoAsync(dto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando solicitud en partición={Particion}", particion);
                }
            }
        }

        /// <summary>
        /// Procesa una solicitud completa: validaciones, actualización de saldo, registro y RabbitMQ.
        /// 
        /// NOTA: Usa Service Locator pattern por necesidad arquitectónica:
        /// - Ejecuta en background thread independiente
        /// - Requiere scope propio para transacciones serializables
        /// - Inyección directa no disponible en contexto asíncrono
        /// </summary>
        private async Task ProcesarSolicitudConActualizacionSaldoAsync(RegistroSolicitudDto dto)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<HandlerDbContext>();
                var cuentaRepository = scope.ServiceProvider.GetRequiredService<ICuentaRepository>();
                var solicitudRepository = scope.ServiceProvider.GetRequiredService<ISolicitudRepository>();
                var publisher = scope.ServiceProvider.GetRequiredService<RabbitMqPublisher>();
                var statusService = scope.ServiceProvider.GetRequiredService<IHandlerStatusService>();
                
                // Migrado: Crear servicios compartidos
                var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                var validationLogger = loggerFactory.CreateLogger<Handler.Shared.SolicitudValidationService>();
                var validationService = new Handler.Shared.SolicitudValidationService(validationLogger);
                
                var saldoLogger = loggerFactory.CreateLogger<Handler.Shared.SaldoCalculationService>();
                var saldoService = new Handler.Shared.SaldoCalculationService(saldoLogger);
                
                var rabbitLogger = loggerFactory.CreateLogger<Handler.Shared.RabbitMqMessageService>();
                var rabbitService = new Handler.Shared.RabbitMqMessageService(publisher, rabbitLogger);
                
                var colaDistService = CrearColaDistributionService();
                
                int reintentos = cantidadReintentos;
                while (reintentos-- > 0)
                {
                    using (var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
                    {
                        try
                        {
                            if (!statusService.EstaActivo())
                                return;

                            // Migrado: Usar servicio de validación en lugar de validaciones manuales
                            var validationResult = validationService.ValidarSolicitud(dto, db);
                            string nombreCola = colaDistService.CalcularNombreCola(dto.NumeroCuenta);
                            
                            var solicitud = new SolicitudDebito
                            {
                                FechaSolicitud = DateTime.UtcNow.Date,
                                FechaReal = DateTime.UtcNow,
                                TipoMovimiento = dto.TipoMovimiento,
                                MovimientoOriginalId = dto.MovimientoOriginalId,
                                NumeroComprobante = dto.NumeroComprobante,
                                Monto = dto.Monto
                            };

                            decimal saldoFinal;
                            
                            if (!validationResult.IsValid)
                            {
                                // Solicitud rechazada
                                solicitud.CuentaId = validationResult.Cuenta?.Id ?? 0;
                                solicitud.Estado = "rechazada";
                                solicitud.CodigoEstado = validationResult.StatusCode;
                                saldoFinal = validationResult.SaldoActual;
                            }
                            else
                            {
                                // Solicitud autorizada
                                var cuenta = validationResult.Cuenta!;
                                solicitud.CuentaId = cuenta.Id;
                                solicitud.Estado = "autorizada";
                                solicitud.CodigoEstado = 0;
                                
                                // Migrado: Usar SaldoCalculationService para aplicar movimiento
                                saldoFinal = saldoService.AplicarMovimiento(cuenta, dto.Monto, dto.TipoMovimiento);
                                cuentaRepository.UpdateAsync(cuenta).Wait();
                            }

                            solicitud.SaldoRespuesta = saldoFinal;
                            solicitudRepository.AddAsync(solicitud).Wait();
                            solicitudRepository.SaveChangesAsync().Wait();

                            // Migrado: Usar RabbitMqMessageService para publicación
                            rabbitService.PublicarSolicitud(solicitud, dto, nombreCola);
                            
                            transaction.Commit();
                            return;
                        }
                        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
                        {
                            transaction.Rollback();
                            if (reintentos == 0)
                            {
                                _logger.LogError(ex, "No se pudo resolver el conflicto para cuenta {NumeroCuenta} después de {CantidadReintentos} reintentos por concurrencia", dto.NumeroCuenta, cantidadReintentos);
                                return;
                            }
                            await Task.Delay(new Random().Next(tiempoMinimoEsperaMs, tiempoMaximoEsperaMs));
                            continue;
                        }
                        catch (Exception ex) when (ex.Message.Contains("deadlock") || ex.Message.Contains("timeout") || ex.Message.Contains("lock"))
                        {
                            transaction.Rollback();
                            if (reintentos == 0)
                            {
                                _logger.LogError(ex, "No se pudo completar la operación para cuenta {NumeroCuenta} después de {CantidadReintentos} reintentos por bloqueo", dto.NumeroCuenta, cantidadReintentos);
                                return;
                            }
                            await Task.Delay(new Random().Next(tiempoMinimoEsperaMs, tiempoMaximoEsperaMs));
                            continue;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError(ex, "Error crítico en ProcesarSolicitudConActualizacionSaldoAsync para cuenta {NumeroCuenta}", dto.NumeroCuenta);
                            return;
                        }
                    }
                }
            }
        }
    }
}