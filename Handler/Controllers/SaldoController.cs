using Microsoft.AspNetCore.Mvc;
using Handler.Services;
using Handler.Controllers.Dtos;

namespace Handler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SaldoController : ControllerBase
    {
        private readonly ISaldoService _saldoService;
        private readonly IHandlerStatusService _statusService;

        public SaldoController(ISaldoService saldoService, IHandlerStatusService statusService)
        {
            _saldoService = saldoService;
            _statusService = statusService;
        }

        [HttpGet("cuenta/{numeroCuenta}")]
        public IActionResult GetSaldoByCuenta(long numeroCuenta)
        {
            if (!_statusService.EstaActivo())
                return StatusCode(503, "El Handler está inactivo.");
            var saldo = _saldoService.GetSaldoByCuenta(numeroCuenta);
            if (saldo == null)
                return NotFound($"No se encontró la cuenta {numeroCuenta}");
            return Ok(saldo);
        }

        [HttpGet("todas")]
        public IActionResult GetSaldoAll()
        {
            if (!_statusService.EstaActivo())
                return StatusCode(503, "El Handler está inactivo.");
            var saldos = _saldoService.GetSaldoAll();
            return Ok(saldos);
        }

        [HttpGet("todas_paginado")]
        public IActionResult GetSaldoPaginado([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (!_statusService.EstaActivo())
                return StatusCode(503, "El Handler está inactivo.");
            var resultado = _saldoService.GetSaldoPaginado(page, pageSize);
            return Ok(resultado);
        }
    }
}
