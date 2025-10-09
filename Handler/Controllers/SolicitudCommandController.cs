using Handler.Controllers.Dtos;
using Handler.Services;
using Microsoft.AspNetCore.Mvc;

namespace Handler.Controllers
{
    [ApiController]
    [Route("api/solicitud-command")]
    public class SolicitudCommandController : ControllerBase
    {
        private readonly ISolicitudCommandQueueService _commandQueueService;

        public SolicitudCommandController(ISolicitudCommandQueueService commandQueueService)
        {
            _commandQueueService = commandQueueService;
        }

        /// <summary>
        /// Encola una solicitud para procesamiento.
        /// La implementación específica depende de la configuración de DI.
        /// </summary>
        [HttpPost]
        public IActionResult EncolarSolicitud([FromBody] RegistroSolicitudDto dto)
        {
            try
            {
                var resultado = _commandQueueService.EncolarSolicitud(dto);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SolicitudCommandController][Error] {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
    }
}
