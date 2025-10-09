using Handler.Controllers.Dtos;

namespace Handler.Shared
{
    /// <summary>
    /// Factory para crear objetos de respuesta de solicitudes con códigos de estado estandarizados.
    /// Centraliza la creación de DTOs de respuesta con consistencia en códigos de estado.
    /// </summary>
    public static class SolicitudResultadoDtoFactory
    {
        /// <summary>
        /// Crea una respuesta de solicitud autorizada exitosa.
        /// </summary>
        /// <param name="id">ID de la solicitud (0 si no se conoce aún)</param>
        /// <param name="saldo">Saldo final de la cuenta</param>
        /// <param name="nombreCola">Nombre de la cola donde se procesó (opcional)</param>
        /// <returns>DTO de respuesta con estado autorizado</returns>
        public static SolicitudResultadoDto CrearAutorizada(int id, decimal saldo, string? nombreCola = null)
        {
            return new SolicitudResultadoDto
            {
                Id = id,
                Saldo = saldo,
                Status = 0, // Autorizada
                Cola = nombreCola ?? string.Empty
            };
        }

        /// <summary>
        /// Crea una respuesta de solicitud rechazada por cuenta no encontrada.
        /// </summary>
        /// <returns>DTO de respuesta con error de cuenta no encontrada</returns>
        public static SolicitudResultadoDto CrearCuentaNoEncontrada()
        {
            return new SolicitudResultadoDto
            {
                Id = 0,
                Saldo = 0,
                Status = 1 // Cuenta no encontrada
            };
        }

        /// <summary>
        /// Crea una respuesta de solicitud rechazada por duplicada.
        /// </summary>
        /// <param name="saldoActual">Saldo actual de la cuenta</param>
        /// <returns>DTO de respuesta con error de solicitud duplicada</returns>
        public static SolicitudResultadoDto CrearSolicitudDuplicada(decimal saldoActual)
        {
            return new SolicitudResultadoDto
            {
                Id = 0,
                Saldo = saldoActual,
                Status = 2 // Solicitud duplicada
            };
        }

        /// <summary>
        /// Crea una respuesta de solicitud rechazada por tipo de movimiento inválido.
        /// </summary>
        /// <param name="saldoActual">Saldo actual de la cuenta</param>
        /// <returns>DTO de respuesta con error de tipo de movimiento inválido</returns>
        public static SolicitudResultadoDto CrearTipoMovimientoInvalido(decimal saldoActual)
        {
            return new SolicitudResultadoDto
            {
                Id = 0,
                Saldo = saldoActual,
                Status = 3 // Tipo de movimiento inválido
            };
        }

        /// <summary>
        /// Crea una respuesta de solicitud rechazada por saldo insuficiente.
        /// </summary>
        /// <param name="saldoActual">Saldo actual de la cuenta</param>
        /// <returns>DTO de respuesta con error de saldo insuficiente</returns>
        public static SolicitudResultadoDto CrearSaldoInsuficiente(decimal saldoActual)
        {
            return new SolicitudResultadoDto
            {
                Id = 0,
                Saldo = saldoActual,
                Status = 4 // Saldo insuficiente
            };
        }

        /// <summary>
        /// Crea una respuesta de solicitud rechazada por servicio inactivo.
        /// </summary>
        /// <returns>DTO de respuesta con error de servicio inactivo</returns>
        public static SolicitudResultadoDto CrearServicioInactivo()
        {
            return new SolicitudResultadoDto
            {
                Id = 0,
                Saldo = 0,
                Status = 5 // Servicio inactivo
            };
        }

        /// <summary>
        /// Crea una respuesta de error por problemas de concurrencia (después de reintentos).
        /// </summary>
        /// <returns>DTO de respuesta con error de concurrencia</returns>
        public static SolicitudResultadoDto CrearErrorConcurrencia()
        {
            return new SolicitudResultadoDto
            {
                Id = 0,
                Saldo = 0,
                Status = 98 // Error de concurrencia
            };
        }

        /// <summary>
        /// Crea una respuesta de error por bloqueos de base de datos (después de reintentos).
        /// </summary>
        /// <returns>DTO de respuesta con error de bloqueo</returns>
        public static SolicitudResultadoDto CrearErrorBloqueo()
        {
            return new SolicitudResultadoDto
            {
                Id = 0,
                Saldo = 0,
                Status = 97 // Error de bloqueo
            };
        }

        /// <summary>
        /// Crea una respuesta de error crítico o inesperado.
        /// </summary>
        /// <returns>DTO de respuesta con error crítico</returns>
        public static SolicitudResultadoDto CrearErrorCritico()
        {
            return new SolicitudResultadoDto
            {
                Id = 0,
                Saldo = 0,
                Status = 99 // Error crítico
            };
        }

        /// <summary>
        /// Crea una respuesta basada en un resultado de validación.
        /// </summary>
        /// <param name="validationResult">Resultado de la validación</param>
        /// <param name="nombreCola">Nombre de la cola (opcional)</param>
        /// <returns>DTO de respuesta correspondiente al resultado de validación</returns>
        public static SolicitudResultadoDto CrearDesdeValidacion(SolicitudValidationService.ValidationResult validationResult, string? nombreCola = null)
        {
            if (validationResult.IsValid)
            {
                return CrearAutorizada(0, validationResult.SaldoActual, nombreCola);
            }

            return validationResult.StatusCode switch
            {
                1 => CrearCuentaNoEncontrada(),
                2 => CrearSolicitudDuplicada(validationResult.SaldoActual),
                3 => CrearTipoMovimientoInvalido(validationResult.SaldoActual),
                4 => CrearSaldoInsuficiente(validationResult.SaldoActual),
                _ => CrearErrorCritico()
            };
        }
    }
}