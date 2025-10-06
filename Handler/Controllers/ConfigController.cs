using Microsoft.AspNetCore.Mvc;
using Handler.Services;
using Handler.Controllers.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace Handler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly IConfigColasService _configColasService;
        private readonly IHandlerStatusService _statusService;

        public ConfigController(IConfigColasService configColasService, IHandlerStatusService statusService)
        {
            _configColasService = configColasService;
            _statusService = statusService;
        }

        /// <summary>
        /// Obtiene la configuración actual de RabbitMQ
        /// </summary>
        [HttpGet("colas")]
        public IActionResult GetColas()
        {
            if (!_statusService.EstaActivo())
                return StatusCode(503, "El Handler está inactivo.");
            try
            {
                var config = _configColasService.GetConfig();
                return Ok(config);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Actualiza la configuración de RabbitMQ (solo nombres de colas)
        /// </summary>
        [HttpPost("colas")]
        public IActionResult SetColas([FromBody] RabbitConfigDto dto)
        {
            if (!_statusService.EstaActivo())
                return StatusCode(503, "El Handler está inactivo.");
            try
            {
                var colasServicios = dto.Colas.Select(c => new ColaDto { Nombre = c.Nombre }).ToList();
                _configColasService.SetColas(colasServicios);
                return Ok(new { resultado = "Colas actualizadas" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Elimina la última cola configurada
        /// </summary>
        [HttpDelete("colas/ultima")]
        public IActionResult EliminarUltimaCola()
        {
            if (!_statusService.EstaActivo())
                return StatusCode(503, "El Handler está inactivo.");
            try
            {
                _configColasService.EliminarUltimaCola();
                return Ok(new { resultado = "Cola eliminada" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Agrega una nueva cola con nombre incremental (cola_N)
        /// </summary>
        [HttpPost("colas/agregar")]
        public IActionResult AgregarCola()
        {
            if (!_statusService.EstaActivo())
                return StatusCode(503, "El Handler está inactivo.");
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var nombre = _configColasService.AgregarCola();
                return Ok(new { resultado = $"Cola '{nombre}' agregada" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    // ...existing code...
}
