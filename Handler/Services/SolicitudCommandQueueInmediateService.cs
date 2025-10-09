using System.Collections.Concurrent;
using System.Data;
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
    /// Servicio de cola de comandos que actualiza saldo inmediatamente con transacción serializable.
    /// Respuesta con saldo actualizado, registro diferido en cola.
    /// </summary>
    public class SolicitudCommandQueueInmediateService : ISolicitudCommandQueueService
    {
        private readonly ConcurrentDictionary<int, Channel<RegistroSolicitudDto>> _colas;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SolicitudCommandQueueInmediateService> _logger;
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
        public SolicitudCommandQueueInmediateService(IServiceProvider serviceProvider, ILogger<SolicitudCommandQueueInmediateService> logger)
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
        /// Evalúa el saldo, aplica la operación y encola la solicitud si es válida.
        /// Graba el saldo final calculado en cuentas usando transacción con aislamiento serializable.
        /// Retorna resultado inmediato con el saldo actualizado.
        /// 
        /// NOTA: Usa Service Locator pattern por necesidad arquitectónica:
        /// - Requiere scope independiente para transacciones serializables
        /// - Manejo de múltiples colas concurrentes
        /// - Inyección directa no es viable en este contexto asíncrono
        /// </summary>
        public SolicitudResultadoDto EncolarSolicitud(RegistroSolicitudDto dto)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HandlerDbContext>();
            var cuentaRepository = scope.ServiceProvider.GetRequiredService<ICuentaRepository>();
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
                using (var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
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
                            transaction.Rollback();
                            return Handler.Shared.SolicitudResultadoDtoFactory.CrearDesdeValidacion(validationResult, nombreCola);
                        }

                        var cuenta = validationResult.Cuenta!;
                        decimal saldoFinal;

                        // Si pasa todas las validaciones, calcular y aplicar el nuevo saldo
                        // Migrado: Usando SaldoCalculationService para aplicar movimiento
                        using var innerScope = _serviceProvider.CreateScope();
                        var saldoLoggerFactory = innerScope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                        var saldoLogger = saldoLoggerFactory.CreateLogger<Handler.Shared.SaldoCalculationService>();
                        var saldoService = new Handler.Shared.SaldoCalculationService(saldoLogger);
                        saldoFinal = saldoService.AplicarMovimiento(cuenta, dto.Monto, dto.TipoMovimiento);
                        
                        // Usar repositorio para actualización
                        cuentaRepository.UpdateAsync(cuenta).Wait();
                        cuentaRepository.SaveChangesAsync().Wait();
                        transaction.Commit();

                        // Encolar la solicitud después de actualizar el saldo
                        int particion = colaDistService.CalcularCola(dto.NumeroCuenta);
                        _colas[particion].Writer.TryWrite(dto);
                        
                        // Migrado: Usando SolicitudResultadoDtoFactory
                        return Handler.Shared.SolicitudResultadoDtoFactory.CrearAutorizada(0, saldoFinal, nombreCola);
                    }
                    catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
                    {
                        transaction.Rollback();
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
                        transaction.Rollback();
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
                        transaction.Rollback();
                        _logger.LogError(ex, "Error crítico en EncolarSolicitud para cuenta {NumeroCuenta}", dto.NumeroCuenta);
                        // Migrado: Usando SolicitudResultadoDtoFactory
                        return Handler.Shared.SolicitudResultadoDtoFactory.CrearErrorCritico();
                    }
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
        /// Procesa la cola de una partición en background para registro diferido.
        /// Solo registra la solicitud y publica en RabbitMQ (saldo ya actualizado).
        /// </summary>
        private async Task ProcesarColaAsync(int particion, Channel<RegistroSolicitudDto> channel)
        {
            await foreach (var dto in channel.Reader.ReadAllAsync())
            {
                try
                {
                    ProcesarSolicitudSoloRegistroAsync(dto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando registro diferido en partición={Particion}", particion);
                }
            }
        }

        /// <summary>
        /// Procesa una solicitud sin actualizar el saldo (ya fue actualizado).
        /// Solo registra la solicitud en la base de datos y publica en RabbitMQ.
        /// 
        /// NOTA: Usa Service Locator pattern por necesidad arquitectónica:
        /// - Ejecuta en background thread independiente
        /// - Requiere scope propio para transacciones serializables
        /// - Inyección directa no disponible en contexto asíncrono
        /// </summary>
        private void ProcesarSolicitudSoloRegistroAsync(RegistroSolicitudDto dto)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<HandlerDbContext>();
                var cuentaRepository = scope.ServiceProvider.GetRequiredService<ICuentaRepository>();
                var solicitudRepository = scope.ServiceProvider.GetRequiredService<ISolicitudRepository>();
                var publisher = scope.ServiceProvider.GetRequiredService<RabbitMqPublisher>();
                var statusService = scope.ServiceProvider.GetRequiredService<IHandlerStatusService>();
                
                // Migrado: Crear servicio de mensajería RabbitMQ
                var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                var rabbitLogger = loggerFactory.CreateLogger<Handler.Shared.RabbitMqMessageService>();
                var rabbitService = new Handler.Shared.RabbitMqMessageService(publisher, rabbitLogger);
                
                int reintentos = cantidadReintentos;
                while (reintentos-- > 0)
                {
                    using (var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
                    {
                        try
                        {
                            if (!statusService.EstaActivo())
                            {
                                transaction.Rollback();
                                return;
                            }

                            // Usar repositorio para consulta
                            var cuenta = cuentaRepository.GetByNumeroAsync(dto.NumeroCuenta).Result;
                            if (cuenta == null)
                            {
                                _logger.LogError("Cuenta {NumeroCuenta} no encontrada en procesamiento diferido", dto.NumeroCuenta);
                                transaction.Rollback();
                                return;
                            }

                            // Crear registro de solicitud sin modificar saldo (ya fue modificado)
                            var solicitud = new SolicitudDebito
                            {
                                CuentaId = cuenta.Id,
                                FechaSolicitud = DateTime.UtcNow.Date,
                                FechaReal = DateTime.UtcNow,
                                TipoMovimiento = dto.TipoMovimiento,
                                MovimientoOriginalId = dto.MovimientoOriginalId,
                                NumeroComprobante = dto.NumeroComprobante,
                                Monto = dto.Monto,
                                Estado = "autorizada", // Si llegó aquí, ya fue autorizada
                                SaldoRespuesta = cuenta.Saldo, // Saldo actual (ya actualizado)
                                CodigoEstado = 0
                            };

                            // Usar repositorio para inserción
                            solicitudRepository.AddAsync(solicitud).Wait();
                            solicitudRepository.SaveChangesAsync().Wait();

                            // Migrado: Usar servicio de distribución de colas reutilizando el helper
                            var colaDistService = CrearColaDistributionService();
                            string nombreCola = colaDistService.CalcularNombreCola(dto.NumeroCuenta);
                            rabbitService.PublicarSolicitud(solicitud, dto, nombreCola);
                            
                            transaction.Commit();
                            return; // Salir exitosamente
                        }
                        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
                        {
                            transaction.Rollback();
                            if (reintentos == 0)
                            {
                                _logger.LogError(ex, "No se pudo registrar solicitud diferida para cuenta {NumeroCuenta} después de {CantidadReintentos} reintentos por concurrencia", dto.NumeroCuenta, cantidadReintentos);
                                return;
                            }
                            _logger.LogWarning(ex, "Conflicto en registro diferido para cuenta {NumeroCuenta}. Reintentando... ({Reintentos} restantes)", dto.NumeroCuenta, reintentos);
                            Thread.Sleep(new Random().Next(tiempoMinimoEsperaMs, tiempoMaximoEsperaMs));
                            continue;
                        }
                        catch (Exception ex) when (ex.Message.Contains("deadlock") || ex.Message.Contains("timeout") || ex.Message.Contains("lock"))
                        {
                            transaction.Rollback();
                            if (reintentos == 0)
                            {
                                _logger.LogError(ex, "No se pudo registrar solicitud diferida para cuenta {NumeroCuenta} después de {CantidadReintentos} reintentos por bloqueo", dto.NumeroCuenta, cantidadReintentos);
                                return;
                            }
                            _logger.LogWarning(ex, "Problema de bloqueo en registro diferido para cuenta {NumeroCuenta}. Reintentando... ({Reintentos} restantes)", dto.NumeroCuenta, reintentos);
                            Thread.Sleep(new Random().Next(tiempoMinimoEsperaMs, tiempoMaximoEsperaMs));
                            continue;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError(ex, "Error crítico en ProcesarSolicitudSoloRegistroAsync para cuenta {NumeroCuenta}", dto.NumeroCuenta);
                            return;
                        }
                    }
                }
            }
        }
    }
}