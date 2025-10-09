using System.Data;
using Handler.Controllers.Dtos;
using Handler.Infrastructure;
using Handler.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Handler.Shared
{
    /// <summary>
    /// Configuración optimizada para transacciones de saldo con mínimos deadlocks.
    /// Implementa estrategias híbridas de aislamiento según el tipo de operación.
    /// </summary>
    public class OptimizedTransactionService
    {
        private readonly ILogger<OptimizedTransactionService> _logger;

        public OptimizedTransactionService(ILogger<OptimizedTransactionService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Configuración de transacción optimizada para operaciones de saldo.
        /// </summary>
        public class TransactionConfig
        {
            public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
            public bool UseExplicitLocking { get; set; } = true;
            public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Ejecuta una operación de saldo con configuración optimizada para reducir deadlocks.
        /// </summary>
        /// <param name="db">Contexto de base de datos</param>
        /// <param name="numeroCuenta">Número de cuenta a procesar</param>
        /// <param name="operation">Operación a ejecutar dentro de la transacción</param>
        /// <param name="config">Configuración de transacción (opcional)</param>
        /// <returns>Resultado de la operación</returns>
        public async Task<T?> ExecuteBalanceOperationAsync<T>(
            HandlerDbContext db,
            long numeroCuenta,
            Func<Cuenta, Task<T>> operation,
            TransactionConfig? config = null)
        {
            config ??= new TransactionConfig();

            using var transaction = db.Database.BeginTransaction(config.IsolationLevel);
            
            try
            {
                // OPTIMIZACIÓN: Usar SELECT con UPDLOCK para evitar deadlocks
                // Esto bloquea la fila inmediatamente pero permite mejores patterns de locking
                Cuenta? cuenta;
                
                if (config.UseExplicitLocking)
                {
                    // Estrategia 1: UPDLOCK explícito (reduce deadlocks significativamente)
                    cuenta = await db.Cuentas
                        .FromSqlRaw("SELECT * FROM Cuentas WITH (UPDLOCK, READPAST) WHERE Numero = {0}", numeroCuenta)
                        .FirstOrDefaultAsync();
                        
                    if (cuenta == null)
                    {
                        // Si no encontramos la cuenta con READPAST, intentar sin él
                        cuenta = await db.Cuentas
                            .FromSqlRaw("SELECT * FROM Cuentas WITH (UPDLOCK) WHERE Numero = {0}", numeroCuenta)
                            .FirstOrDefaultAsync();
                    }
                }
                else
                {
                    // Estrategia 2: Locking tradicional de EF Core
                    cuenta = await db.Cuentas.FirstOrDefaultAsync(c => c.Numero == numeroCuenta);
                }

                if (cuenta == null)
                {
                    transaction.Rollback();
                    return default;
                }

                // Ejecutar operación del usuario
                var result = await operation(cuenta);

                // Commit optimista
                await db.SaveChangesAsync();
                transaction.Commit();

                _logger.LogDebug("Operación de saldo completada exitosamente para cuenta {NumeroCuenta}", numeroCuenta);
                return result;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error en operación de saldo para cuenta {NumeroCuenta}", numeroCuenta);
                throw;
            }
        }

        /// <summary>
        /// Versión síncrona de la operación de saldo optimizada.
        /// </summary>
        public T? ExecuteBalanceOperation<T>(
            HandlerDbContext db,
            long numeroCuenta,
            Func<Cuenta, T> operation,
            TransactionConfig? config = null)
        {
            config ??= new TransactionConfig();

            using var transaction = db.Database.BeginTransaction(config.IsolationLevel);
            
            try
            {
                Cuenta? cuenta;
                
                if (config.UseExplicitLocking)
                {
                    cuenta = db.Cuentas
                        .FromSqlRaw("SELECT * FROM Cuentas WITH (UPDLOCK, READPAST) WHERE Numero = {0}", numeroCuenta)
                        .FirstOrDefault();
                        
                    if (cuenta == null)
                    {
                        cuenta = db.Cuentas
                            .FromSqlRaw("SELECT * FROM Cuentas WITH (UPDLOCK) WHERE Numero = {0}", numeroCuenta)
                            .FirstOrDefault();
                    }
                }
                else
                {
                    cuenta = db.Cuentas.FirstOrDefault(c => c.Numero == numeroCuenta);
                }

                if (cuenta == null)
                {
                    transaction.Rollback();
                    return default;
                }

                var result = operation(cuenta);

                db.SaveChanges();
                transaction.Commit();

                _logger.LogDebug("Operación de saldo completada exitosamente para cuenta {NumeroCuenta}", numeroCuenta);
                return result;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error en operación de saldo para cuenta {NumeroCuenta}", numeroCuenta);
                throw;
            }
        }

        /// <summary>
        /// Configuración para operaciones de alta concurrencia.
        /// Prioriza performance sobre consistencia absoluta.
        /// </summary>
        public static TransactionConfig HighConcurrencyConfig => new()
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            UseExplicitLocking = true,
            Timeout = TimeSpan.FromSeconds(5)
        };

        /// <summary>
        /// Configuración para operaciones de alta consistencia.
        /// Prioriza consistencia sobre performance.
        /// </summary>
        public static TransactionConfig HighConsistencyConfig => new()
        {
            IsolationLevel = IsolationLevel.RepeatableRead,
            UseExplicitLocking = true,
            Timeout = TimeSpan.FromSeconds(15)
        };

        /// <summary>
        /// Configuración por defecto balanceada.
        /// </summary>
        public static TransactionConfig DefaultConfig => new()
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            UseExplicitLocking = true,
            Timeout = TimeSpan.FromSeconds(10)
        };
    }
}