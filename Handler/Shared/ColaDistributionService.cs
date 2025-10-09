using Handler.Controllers.Dtos;
using Microsoft.Extensions.Logging;

namespace Handler.Shared
{
    /// <summary>
    /// Servicio para cálculo de distribución de colas y algoritmos de particionado.
    /// Centraliza la lógica de distribución de solicitudes entre múltiples colas.
    /// </summary>
    public class ColaDistributionService
    {
        private readonly ILogger<ColaDistributionService> _logger;
        private readonly int _cantidadColas;

        public ColaDistributionService(int cantidadColas, ILogger<ColaDistributionService> logger)
        {
            _cantidadColas = cantidadColas;
            _logger = logger;
        }

        /// <summary>
        /// Calcula la cola destino para una cuenta específica usando algoritmo de módulo.
        /// Garantiza distribución uniforme de las solicitudes entre las colas disponibles.
        /// </summary>
        /// <param name="numeroCuenta">Número de cuenta para calcular la partición</param>
        /// <returns>Número de cola (1-based)</returns>
        public int CalcularCola(long numeroCuenta)
        {
            if (_cantidadColas <= 0)
            {
                _logger.LogDebug("CantidadColas es {CantidadColas}, usando cola por defecto: cola_1", _cantidadColas);
                return 1;
            }
            
            int resultadoModulo = (int)(numeroCuenta % _cantidadColas);
            int colaDestino = resultadoModulo + 1;
            
            _logger.LogDebug("Cuenta {NumeroCuenta} -> Cola {ColaDestino} (módulo {Modulo})", 
                numeroCuenta, colaDestino, resultadoModulo);
            
            return colaDestino;
        }

        /// <summary>
        /// Obtiene el nombre de la cola basado en su número.
        /// </summary>
        /// <param name="numeroCola">Número de cola (1-based)</param>
        /// <returns>Nombre de la cola en formato "cola_X"</returns>
        public string ObtenerNombreCola(int numeroCola)
        {
            return $"cola_{numeroCola}";
        }

        /// <summary>
        /// Calcula la cola y devuelve su nombre directamente.
        /// </summary>
        /// <param name="numeroCuenta">Número de cuenta</param>
        /// <returns>Nombre de la cola en formato "cola_X"</returns>
        public string CalcularNombreCola(long numeroCuenta)
        {
            int numeroCola = CalcularCola(numeroCuenta);
            return ObtenerNombreCola(numeroCola);
        }
    }
}