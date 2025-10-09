using Handler.Controllers.Dtos;
using Handler.Infrastructure;
using Handler.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Handler.Shared
{
    /// <summary>
    /// Servicio compartido para validaciones comunes de solicitudes de débito/crédito.
    /// Encapsula la lógica de validación reutilizable entre diferentes implementaciones.
    /// </summary>
    public class SolicitudValidationService
    {
        private readonly ILogger<SolicitudValidationService> _logger;

        public SolicitudValidationService(ILogger<SolicitudValidationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Resultado de validación de una solicitud.
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public int StatusCode { get; set; }
            public string? ErrorMessage { get; set; }
            public Cuenta? Cuenta { get; set; }
            public decimal SaldoActual { get; set; }
        }

        /// <summary>
        /// Valida una solicitud completa incluyendo cuenta, idempotencia, tipo de movimiento y saldo.
        /// </summary>
        /// <param name="dto">Datos de la solicitud a validar</param>
        /// <param name="db">Contexto de base de datos</param>
        /// <returns>Resultado de validación con detalles</returns>
        public ValidationResult ValidarSolicitud(RegistroSolicitudDto dto, HandlerDbContext db)
        {
            var result = new ValidationResult();

            // 1. Validar cuenta existente
            var cuenta = db.Cuentas.FirstOrDefault(c => c.Numero == dto.NumeroCuenta);
            if (cuenta is null)
            {
                result.IsValid = false;
                result.StatusCode = 1;
                result.ErrorMessage = "Cuenta no encontrada";
                result.SaldoActual = 0;
                return result;
            }

            result.Cuenta = cuenta;
            result.SaldoActual = cuenta.Saldo;

            // 2. Control de idempotencia
            if (ExisteSolicitudDuplicada(dto, cuenta.Id, db))
            {
                result.IsValid = false;
                result.StatusCode = 2;
                result.ErrorMessage = "Solicitud duplicada";
                return result;
            }

            // 3. Validar tipo de movimiento
            if (!EsTipoMovimientoValido(dto.TipoMovimiento))
            {
                result.IsValid = false;
                result.StatusCode = 3;
                result.ErrorMessage = "Tipo de movimiento inválido";
                return result;
            }

            // 4. Validar saldo suficiente para débitos
            if (RequiereValidacionSaldo(dto.TipoMovimiento) && cuenta.Saldo < dto.Monto)
            {
                result.IsValid = false;
                result.StatusCode = 4;
                result.ErrorMessage = "Saldo insuficiente";
                return result;
            }

            // Si llega aquí, la solicitud es válida
            result.IsValid = true;
            result.StatusCode = 0;
            return result;
        }

        /// <summary>
        /// Verifica si existe una solicitud autorizada duplicada en el día actual.
        /// </summary>
        private bool ExisteSolicitudDuplicada(RegistroSolicitudDto dto, int cuentaId, HandlerDbContext db)
        {
            return db.SolicitudesDebito.Any(s =>
                s.CuentaId == cuentaId &&
                s.Monto == dto.Monto &&
                s.NumeroComprobante == dto.NumeroComprobante &&
                s.FechaSolicitud.Date == DateTime.UtcNow.Date &&
                s.Estado == "autorizada");
        }

        /// <summary>
        /// Verifica si el tipo de movimiento es válido.
        /// </summary>
        private bool EsTipoMovimientoValido(string tipoMovimiento)
        {
            return tipoMovimiento is "debito" or "credito" or "contrasiento_debito" or "contrasiento_credito";
        }

        /// <summary>
        /// Determina si el tipo de movimiento requiere validación de saldo.
        /// </summary>
        private bool RequiereValidacionSaldo(string tipoMovimiento)
        {
            return tipoMovimiento is "debito" or "contrasiento_credito";
        }
    }
}