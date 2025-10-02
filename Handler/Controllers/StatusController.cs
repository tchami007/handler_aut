using Microsoft.AspNetCore.Mvc;
using Handler.Services;
using Microsoft.AspNetCore.Authorization;

namespace Handler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly IHandlerStatusService _statusService;

        public StatusController(IHandlerStatusService statusService)
        {
            _statusService = statusService;
        }

        /// <summary>
        /// Endpoint de salud del Handler
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            var info = _statusService.GetHealthInfo();
            return Ok(info);
        }

        /// <summary>
        /// Activa el Handler
        /// </summary>

        [HttpPost("activar")]
        public IActionResult Activar()
        {
            _statusService.Activar();
            return Ok(new { estado = "activo" });
        }

        /// <summary>
        /// Inactiva el Handler
        /// </summary>

        [HttpPost("inactivar")]
        public IActionResult Inactivar()
        {
            _statusService.Inactivar();
            return Ok(new { estado = "inactivo" });
        }
    }
}
