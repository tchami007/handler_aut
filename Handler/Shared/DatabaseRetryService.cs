using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Handler.Shared
{
    /// <summary>
    /// Configuración para operaciones que requieren reintentos por problemas de concurrencia.
    /// </summary>
    public class RetryConfiguration
    {
        public int CantidadReintentos { get; set; } = 10;
        public int TiempoMinimoEsperaMs { get; set; } = 50;
        public int TiempoMaximoEsperaMs { get; set; } = 100;
    }

    /// <summary>
    /// Servicio para manejo de reintentos en operaciones de base de datos con problemas de concurrencia.
    /// Implementa patrones de retry con exponential backoff y manejo específico de excepciones de BD.
    /// </summary>
    public class DatabaseRetryService
    {
        private readonly ILogger<DatabaseRetryService> _logger;
        private readonly RetryConfiguration _config;

        public DatabaseRetryService(ILogger<DatabaseRetryService> logger, RetryConfiguration? config = null)
        {
            _logger = logger;
            _config = config ?? new RetryConfiguration();
        }

        /// <summary>
        /// Ejecuta una operación con reintentos automáticos en caso de problemas de concurrencia.
        /// </summary>
        /// <typeparam name="T">Tipo de resultado de la operación</typeparam>
        /// <param name="operation">Operación a ejecutar</param>
        /// <param name="operationName">Nombre descriptivo de la operación para logging</param>
        /// <param name="contextInfo">Información adicional de contexto para logging</param>
        /// <returns>Resultado de la operación o valor por defecto si fallan todos los reintentos</returns>
        public async Task<T?> ExecuteWithRetryAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            string? contextInfo = null)
        {
            int reintentos = _config.CantidadReintentos;
            var random = new Random();

            while (reintentos-- > 0)
            {
                try
                {
                    return await operation();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (reintentos == 0)
                    {
                        _logger.LogError(ex, "No se pudo resolver el conflicto en {OperationName} después de {CantidadReintentos} reintentos por concurrencia. Contexto: {ContextInfo}", 
                            operationName, _config.CantidadReintentos, contextInfo);
                        return default;
                    }

                    int tiempoEspera = random.Next(_config.TiempoMinimoEsperaMs, _config.TiempoMaximoEsperaMs);
                    _logger.LogWarning("Conflicto de concurrencia en {OperationName}, reintentando en {TiempoEspera}ms. Reintentos restantes: {ReintentosRestantes}. Contexto: {ContextInfo}", 
                        operationName, tiempoEspera, reintentos, contextInfo);
                    
                    await Task.Delay(tiempoEspera);
                }
                catch (Exception ex) when (IsDatabaseLockException(ex))
                {
                    if (reintentos == 0)
                    {
                        _logger.LogError(ex, "No se pudo completar {OperationName} después de {CantidadReintentos} reintentos por bloqueo de base de datos. Contexto: {ContextInfo}", 
                            operationName, _config.CantidadReintentos, contextInfo);
                        return default;
                    }

                    int tiempoEspera = random.Next(_config.TiempoMinimoEsperaMs, _config.TiempoMaximoEsperaMs);
                    _logger.LogWarning("Bloqueo de base de datos en {OperationName}, reintentando en {TiempoEspera}ms. Reintentos restantes: {ReintentosRestantes}. Contexto: {ContextInfo}", 
                        operationName, tiempoEspera, reintentos, contextInfo);
                    
                    await Task.Delay(tiempoEspera);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error crítico en {OperationName}. Contexto: {ContextInfo}", operationName, contextInfo);
                    return default;
                }
            }

            _logger.LogError("Se agotaron todos los reintentos para {OperationName}. Contexto: {ContextInfo}", operationName, contextInfo);
            return default;
        }

        /// <summary>
        /// Versión síncrona del método ExecuteWithRetryAsync.
        /// </summary>
        public T? ExecuteWithRetry<T>(
            Func<T> operation,
            string operationName,
            string? contextInfo = null)
        {
            int reintentos = _config.CantidadReintentos;
            var random = new Random();

            while (reintentos-- > 0)
            {
                try
                {
                    return operation();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (reintentos == 0)
                    {
                        _logger.LogError(ex, "No se pudo resolver el conflicto en {OperationName} después de {CantidadReintentos} reintentos por concurrencia. Contexto: {ContextInfo}", 
                            operationName, _config.CantidadReintentos, contextInfo);
                        return default;
                    }

                    int tiempoEspera = random.Next(_config.TiempoMinimoEsperaMs, _config.TiempoMaximoEsperaMs);
                    _logger.LogWarning("Conflicto de concurrencia en {OperationName}, reintentando en {TiempoEspera}ms. Reintentos restantes: {ReintentosRestantes}. Contexto: {ContextInfo}", 
                        operationName, tiempoEspera, reintentos, contextInfo);
                    
                    Task.Delay(tiempoEspera).Wait();
                }
                catch (Exception ex) when (IsDatabaseLockException(ex))
                {
                    if (reintentos == 0)
                    {
                        _logger.LogError(ex, "No se pudo completar {OperationName} después de {CantidadReintentos} reintentos por bloqueo de base de datos. Contexto: {ContextInfo}", 
                            operationName, _config.CantidadReintentos, contextInfo);
                        return default;
                    }

                    int tiempoEspera = random.Next(_config.TiempoMinimoEsperaMs, _config.TiempoMaximoEsperaMs);
                    _logger.LogWarning("Bloqueo de base de datos en {OperationName}, reintentando en {TiempoEspera}ms. Reintentos restantes: {ReintentosRestantes}. Contexto: {ContextInfo}", 
                        operationName, tiempoEspera, reintentos, contextInfo);
                    
                    Task.Delay(tiempoEspera).Wait();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error crítico en {OperationName}. Contexto: {ContextInfo}", operationName, contextInfo);
                    return default;
                }
            }

            _logger.LogError("Se agotaron todos los reintentos para {OperationName}. Contexto: {ContextInfo}", operationName, contextInfo);
            return default;
        }

        /// <summary>
        /// Determina si una excepción está relacionada con bloqueos de base de datos.
        /// </summary>
        private static bool IsDatabaseLockException(Exception ex)
        {
            return ex.Message.Contains("deadlock", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("lock", StringComparison.OrdinalIgnoreCase);
        }
    }
}