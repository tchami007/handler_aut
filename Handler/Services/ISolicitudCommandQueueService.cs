using Handler.Controllers.Dtos;

namespace Handler.Services
{
    /// <summary>
    /// Interfaz común para servicios de cola de comandos de solicitudes.
    /// Permite intercambiar implementaciones desde el controlador.
    /// </summary>
    public interface ISolicitudCommandQueueService
    {
        /// <summary>
        /// Encola una solicitud para procesamiento.
        /// </summary>
        /// <param name="dto">Datos de la solicitud a encolar</param>
        /// <returns>Resultado de la operación con saldo y estado</returns>
        SolicitudResultadoDto EncolarSolicitud(RegistroSolicitudDto dto);
    }
}