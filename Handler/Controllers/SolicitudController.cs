using Microsoft.AspNetCore.Mvc;
using Handler.Services;
using Handler.Controllers.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace Handler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SolicitudController : ControllerBase
    {
        private readonly ISolicitudService _solicitudService;
        public SolicitudController(ISolicitudService solicitudService)
        {
            _solicitudService = solicitudService;
        }

        [HttpPost]
        public IActionResult Registrar([FromBody] RegistroSolicitudDto dto)
        {
            try
            {
                var resultado = _solicitudService.RegistrarSolicitudConSaldo(dto);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Recupera todas las solicitudes procesadas, ordenadas por fecha real de procesamiento descendente.
        /// </summary>
        [HttpGet("procesadas")]
        public IActionResult GetSolicitudesProcesadas()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                Console.WriteLine($"[Seguimiento] Inicio endpoint GetSolicitudesProcesadas: {DateTime.UtcNow:O}");
                sw.Restart();
                var solicitudes = _solicitudService.GetSolicitudesProcesadas();
                sw.Stop();
                Console.WriteLine($"[Seguimiento] Fin consulta EF, duración: {sw.ElapsedMilliseconds} ms");
                sw.Restart();
                var result = Ok(solicitudes);
                sw.Stop();
                Console.WriteLine($"[Seguimiento] Fin serialización y respuesta, duración: {sw.ElapsedMilliseconds} ms");
                Console.WriteLine($"[Seguimiento] Fin endpoint GetSolicitudesProcesadas: {DateTime.UtcNow:O}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Seguimiento] Error en endpoint GetSolicitudesProcesadas: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
    }
}
