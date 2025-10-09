using System.Data;
using Handler.Controllers.Dtos;
using Handler.Infrastructure;
using Handler.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Handler.Shared
{
    /// <summary>
    /// Servicio de alto nivel que orchestrra todas las operaciones comunes para el procesamiento de solicitudes.
    /// Combina validación, cálculo de saldos, distribución de colas y manejo de reintentos.
    /// </summary>
    public class SolicitudProcessingService
    {
        private readonly SolicitudValidationService _validationService;
        private readonly SaldoCalculationService _saldoService;
        private readonly ColaDistributionService _colaService;
        private readonly RabbitMqMessageService _rabbitService;
        private readonly DatabaseRetryService _retryService;
        private readonly OptimizedTransactionService _transactionService;
        private readonly ILogger<SolicitudProcessingService> _logger;

        public SolicitudProcessingService(
            SolicitudValidationService validationService,
            SaldoCalculationService saldoService,
            ColaDistributionService colaService,
            RabbitMqMessageService rabbitService,
            DatabaseRetryService retryService,
            OptimizedTransactionService transactionService,
            ILogger<SolicitudProcessingService> logger)
        {
            _validationService = validationService;
            _saldoService = saldoService;
            _colaService = colaService;
            _rabbitService = rabbitService;
            _retryService = retryService;
            _transactionService = transactionService;
            _logger = logger;
        }

        /// <summary>
        /// Procesa una solicitud completa usando transacciones optimizadas para reducir deadlocks.
        /// </summary>
        public async Task<SolicitudResultadoDto> ProcesarSolicitudOptimizadaAsync(
            RegistroSolicitudDto dto, 
            HandlerDbContext db, 
            bool actualizarSaldoInmediatamente = true)
        {
            return await _retryService.ExecuteWithRetryAsync(
                async () => await ProcesarSolicitudConTransaccionOptimizadaAsync(dto, db, actualizarSaldoInmediatamente),
                "ProcesarSolicitudOptimizada",
                $"Cuenta: {dto.NumeroCuenta}, Monto: {dto.Monto}") 
                ?? SolicitudResultadoDtoFactory.CrearErrorCritico();
        }

        /// <summary>
        /// Implementación interna con transacciones optimizadas.
        /// </summary>
        private async Task<SolicitudResultadoDto> ProcesarSolicitudConTransaccionOptimizadaAsync(
            RegistroSolicitudDto dto, 
            HandlerDbContext db, 
            bool actualizarSaldoInmediatamente)
        {
            // Configuración de transacción según el tipo de operación
            var config = actualizarSaldoInmediatamente 
                ? OptimizedTransactionService.HighConcurrencyConfig 
                : OptimizedTransactionService.DefaultConfig;

            return await _transactionService.ExecuteBalanceOperationAsync(db, dto.NumeroCuenta, async (cuenta) =>
            {
                // 1. Validar dentro de la transacción (cuenta ya bloqueada)
                var validationResult = ValidarSolicitudConCuenta(dto, cuenta, db);
                string nombreCola = _colaService.CalcularNombreCola(dto.NumeroCuenta);
                
                if (!validationResult.IsValid)
                {
                    return SolicitudResultadoDtoFactory.CrearDesdeValidacion(validationResult, nombreCola);
                }

                decimal saldoFinal = cuenta.Saldo;

                // 2. Actualizar saldo si es necesario
                if (actualizarSaldoInmediatamente)
                {
                    saldoFinal = _saldoService.AplicarMovimiento(cuenta, dto.Monto, dto.TipoMovimiento);
                    db.Cuentas.Update(cuenta);
                }

                // 3. Crear y guardar solicitud
                var solicitud = CrearSolicitudDebito(dto, cuenta, saldoFinal, "autorizada", 0);
                db.SolicitudesDebito.Add(solicitud);
                await db.SaveChangesAsync();

                // 4. Publicar en RabbitMQ (fuera de transacción crítica)
                try
                {
                    _rabbitService.PublicarSolicitud(solicitud, dto, nombreCola);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error publicando en RabbitMQ, pero transacción exitosa para solicitud {SolicitudId}", solicitud.Id);
                    // No fallar la transacción por errores de messaging
                }

                _logger.LogDebug("Solicitud procesada con transacción optimizada: ID={SolicitudId}, Cuenta={NumeroCuenta}, Saldo={SaldoFinal}", 
                    solicitud.Id, dto.NumeroCuenta, saldoFinal);

                return SolicitudResultadoDtoFactory.CrearAutorizada(solicitud.Id, saldoFinal, nombreCola);
            }, config) ?? SolicitudResultadoDtoFactory.CrearErrorCritico();
        }

        /// <summary>
        /// Validación rápida con cuenta ya cargada (evita consultas adicionales).
        /// </summary>
        private SolicitudValidationService.ValidationResult ValidarSolicitudConCuenta(
            RegistroSolicitudDto dto, 
            Cuenta cuenta, 
            HandlerDbContext db)
        {
            var result = new SolicitudValidationService.ValidationResult
            {
                Cuenta = cuenta,
                SaldoActual = cuenta.Saldo
            };

            // Control de idempotencia
            var existe = db.SolicitudesDebito.Any(s =>
                s.CuentaId == cuenta.Id &&
                s.Monto == dto.Monto &&
                s.NumeroComprobante == dto.NumeroComprobante &&
                s.FechaSolicitud.Date == DateTime.UtcNow.Date &&
                s.Estado == "autorizada");
                
            if (existe)
            {
                result.IsValid = false;
                result.StatusCode = 2;
                result.ErrorMessage = "Solicitud duplicada";
                return result;
            }

            // Validar tipo de movimiento
            if (dto.TipoMovimiento != "debito" && dto.TipoMovimiento != "credito" && 
                dto.TipoMovimiento != "contrasiento_debito" && dto.TipoMovimiento != "contrasiento_credito")
            {
                result.IsValid = false;
                result.StatusCode = 3;
                result.ErrorMessage = "Tipo de movimiento inválido";
                return result;
            }

            // Validar saldo suficiente
            if ((dto.TipoMovimiento == "debito" || dto.TipoMovimiento == "contrasiento_credito") && cuenta.Saldo < dto.Monto)
            {
                result.IsValid = false;
                result.StatusCode = 4;
                result.ErrorMessage = "Saldo insuficiente";
                return result;
            }

            result.IsValid = true;
            result.StatusCode = 0;
            return result;
        }

        /// <summary>
        /// Valida una solicitud rápidamente sin procesarla.
        /// </summary>
        /// <param name="dto">Datos de la solicitud</param>
        /// <param name="db">Contexto de base de datos</param>
        /// <returns>Resultado de la validación</returns>
        public SolicitudResultadoDto ValidarSolicitudRapida(RegistroSolicitudDto dto, HandlerDbContext db)
        {
            var validationResult = _validationService.ValidarSolicitud(dto, db);
            string nombreCola = _colaService.CalcularNombreCola(dto.NumeroCuenta);
            
            return SolicitudResultadoDtoFactory.CrearDesdeValidacion(validationResult, nombreCola);
        }

        /// <summary>
        /// Crea una entidad SolicitudDebito basada en los datos del DTO y resultado de validación.
        /// </summary>
        /// <param name="dto">Datos de la solicitud</param>
        /// <param name="cuenta">Cuenta asociada</param>
        /// <param name="saldoFinal">Saldo final calculado</param>
        /// <param name="estado">Estado de la solicitud</param>
        /// <param name="codigoEstado">Código de estado numérico</param>
        /// <returns>Entidad SolicitudDebito configurada</returns>
        public SolicitudDebito CrearSolicitudDebito(
            RegistroSolicitudDto dto, 
            Cuenta? cuenta, 
            decimal saldoFinal, 
            string estado, 
            int codigoEstado)
        {
            return new SolicitudDebito
            {
                CuentaId = cuenta?.Id ?? 0,
                FechaSolicitud = DateTime.UtcNow.Date,
                FechaReal = DateTime.UtcNow,
                TipoMovimiento = dto.TipoMovimiento,
                MovimientoOriginalId = dto.MovimientoOriginalId,
                NumeroComprobante = dto.NumeroComprobante,
                Monto = dto.Monto,
                Estado = estado,
                SaldoRespuesta = saldoFinal,
                CodigoEstado = codigoEstado
            };
        }

        /// <summary>
        /// Implementación interna del procesamiento de solicitud.
        /// </summary>
        private async Task<SolicitudResultadoDto> ProcesarSolicitudInternaAsync(
            RegistroSolicitudDto dto, 
            HandlerDbContext db, 
            bool actualizarSaldoInmediatamente)
        {
            // CRÍTICO: Usar aislamiento serializable para garantizar consistencia de saldo
            using var transaction = actualizarSaldoInmediatamente 
                ? db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable)
                : db.Database.BeginTransaction();

            try
            {
                // 1. Validar solicitud
                var validationResult = _validationService.ValidarSolicitud(dto, db);
                string nombreCola = _colaService.CalcularNombreCola(dto.NumeroCuenta);
                
                if (!validationResult.IsValid)
                {
                    transaction.Rollback();
                    return SolicitudResultadoDtoFactory.CrearDesdeValidacion(validationResult, nombreCola);
                }

                var cuenta = validationResult.Cuenta!;
                decimal saldoFinal = validationResult.SaldoActual;

                // 2. Actualizar saldo si es necesario
                if (actualizarSaldoInmediatamente)
                {
                    saldoFinal = _saldoService.AplicarMovimiento(cuenta, dto.Monto, dto.TipoMovimiento);
                    db.Cuentas.Update(cuenta);
                }

                // 3. Crear y guardar solicitud
                var solicitud = CrearSolicitudDebito(dto, cuenta, saldoFinal, "autorizada", 0);
                db.SolicitudesDebito.Add(solicitud);
                await db.SaveChangesAsync();

                // 4. Publicar en RabbitMQ
                _rabbitService.PublicarSolicitud(solicitud, dto, nombreCola);

                transaction.Commit();

                _logger.LogDebug("Solicitud procesada exitosamente: ID={SolicitudId}, Cuenta={NumeroCuenta}, Saldo={SaldoFinal}", 
                    solicitud.Id, dto.NumeroCuenta, saldoFinal);

                return SolicitudResultadoDtoFactory.CrearAutorizada(solicitud.Id, saldoFinal, nombreCola);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error en procesamiento interno de solicitud para cuenta {NumeroCuenta}", dto.NumeroCuenta);
                throw;
            }
        }
    }
}