using Handler.Models;
using Microsoft.Extensions.Logging;

namespace Handler.Shared
{
    /// <summary>
    /// Servicio para operaciones de actualización de saldos de cuentas.
    /// Centraliza la lógica de cálculo y aplicación de movimientos financieros.
    /// </summary>
    public class SaldoCalculationService
    {
        private readonly ILogger<SaldoCalculationService> _logger;

        public SaldoCalculationService(ILogger<SaldoCalculationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Calcula el nuevo saldo de una cuenta basado en el tipo de movimiento y monto.
        /// No modifica la cuenta, solo calcula el resultado.
        /// </summary>
        /// <param name="saldoActual">Saldo actual de la cuenta</param>
        /// <param name="monto">Monto de la operación</param>
        /// <param name="tipoMovimiento">Tipo de movimiento (debito, credito, contrasiento_debito, contrasiento_credito)</param>
        /// <returns>Nuevo saldo calculado</returns>
        public decimal CalcularNuevoSaldo(decimal saldoActual, decimal monto, string tipoMovimiento)
        {
            decimal nuevoSaldo = tipoMovimiento switch
            {
                "debito" or "contrasiento_credito" => saldoActual - monto,
                "credito" or "contrasiento_debito" => saldoActual + monto,
                _ => throw new ArgumentException($"Tipo de movimiento no válido: {tipoMovimiento}", nameof(tipoMovimiento))
            };

            _logger.LogDebug("Cálculo de saldo: {SaldoActual} -> {NuevoSaldo} (Movimiento: {TipoMovimiento}, Monto: {Monto})", 
                saldoActual, nuevoSaldo, tipoMovimiento, monto);

            return nuevoSaldo;
        }

        /// <summary>
        /// Aplica un movimiento a una cuenta, actualizando su saldo.
        /// Modifica directamente la entidad cuenta.
        /// </summary>
        /// <param name="cuenta">Cuenta a modificar</param>
        /// <param name="monto">Monto de la operación</param>
        /// <param name="tipoMovimiento">Tipo de movimiento</param>
        /// <returns>Nuevo saldo de la cuenta</returns>
        public decimal AplicarMovimiento(Cuenta cuenta, decimal monto, string tipoMovimiento)
        {
            if (cuenta is null)
                throw new ArgumentNullException(nameof(cuenta));

            decimal saldoAnterior = cuenta.Saldo;
            cuenta.Saldo = CalcularNuevoSaldo(cuenta.Saldo, monto, tipoMovimiento);

            _logger.LogDebug("Movimiento aplicado en cuenta {CuentaId}: {SaldoAnterior} -> {SaldoNuevo} (Tipo: {TipoMovimiento}, Monto: {Monto})", 
                cuenta.Id, saldoAnterior, cuenta.Saldo, tipoMovimiento, monto);

            return cuenta.Saldo;
        }

        /// <summary>
        /// Verifica si un tipo de movimiento requiere validación de saldo disponible.
        /// </summary>
        /// <param name="tipoMovimiento">Tipo de movimiento a verificar</param>
        /// <returns>True si requiere validación de saldo, False en caso contrario</returns>
        public bool RequiereValidacionSaldo(string tipoMovimiento)
        {
            return tipoMovimiento is "debito" or "contrasiento_credito";
        }

        /// <summary>
        /// Verifica si hay saldo suficiente para realizar un movimiento que lo requiera.
        /// </summary>
        /// <param name="saldoActual">Saldo actual de la cuenta</param>
        /// <param name="monto">Monto de la operación</param>
        /// <param name="tipoMovimiento">Tipo de movimiento</param>
        /// <returns>True si hay saldo suficiente o no se requiere validación, False si hay saldo insuficiente</returns>
        public bool TieneSaldoSuficiente(decimal saldoActual, decimal monto, string tipoMovimiento)
        {
            if (!RequiereValidacionSaldo(tipoMovimiento))
                return true;

            return saldoActual >= monto;
        }
    }
}