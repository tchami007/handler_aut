using Handler.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Handler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EstadisticaController : ControllerBase
    {
        private readonly IEstadisticaService _estadisticaService;
        private readonly IHandlerStatusService _statusService;

        public EstadisticaController(IEstadisticaService estadisticaService, IHandlerStatusService statusService)
        {
            _estadisticaService = estadisticaService;
            _statusService = statusService;
        }

        /// <summary>
        /// Obtiene estadísticas agregadas del Handler. Permite filtrar por tipo de movimiento.
        /// </summary>
        /// <param name="tipoMovimiento">Opcional: debito, credito, contrasiento_debito, contrasiento_credito</param>
        [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? tipoMovimiento = null)
        {
            if (!_statusService.EstaActivo())
                return StatusCode(503, "El Handler está inactivo.");
            var estadisticas = await _estadisticaService.GetEstadisticasAsync(tipoMovimiento);
            return Ok(estadisticas);
        }
    }
}
