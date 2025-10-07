using Handler.Controllers.Dtos;

namespace Handler.Services
{
    public interface ISolicitudService
    {
    SolicitudResultadoDto RegistrarSolicitudConSaldo(RegistroSolicitudDto dto);

    /// <summary>
    /// Recupera todas las solicitudes procesadas, ordenadas por fecha real de procesamiento descendente.
    /// </summary>
    List<SolicitudDebitoDto> GetSolicitudesProcesadas();
        /// <summary>
        /// Recupera todas las solicitudes procesadas para una cuenta específica, ordenadas por fecha real ascendente.
        /// </summary>
        List<SolicitudDebitoDto> GetSolicitudesPorCuenta(long numeroCuenta);
    }
}
