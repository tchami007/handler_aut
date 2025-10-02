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

        public EstadisticaController(IEstadisticaService estadisticaService)
        {
            _estadisticaService = estadisticaService;
        }

        /// <summary>
        /// Obtiene estadísticas agregadas del Handler. Permite filtrar por tipo de movimiento.
        /// </summary>
        /// <param name="tipoMovimiento">Opcional: debito, credito, contrasiento_debito, contrasiento_credito</param>
        [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? tipoMovimiento = null)
        {
            var estadisticas = await _estadisticaService.GetEstadisticasAsync(tipoMovimiento);
            return Ok(estadisticas);
        }
    }
}
