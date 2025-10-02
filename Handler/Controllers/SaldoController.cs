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

        public SaldoController(ISaldoService saldoService)
        {
            _saldoService = saldoService;
        }

        [HttpGet("cuenta/{numeroCuenta}")]
        public IActionResult GetSaldoByCuenta(long numeroCuenta)
        {
            var saldo = _saldoService.GetSaldoByCuenta(numeroCuenta);
            if (saldo == null)
                return NotFound($"No se encontró la cuenta {numeroCuenta}");
            return Ok(saldo);
        }

        [HttpGet("todas")]
        public IActionResult GetSaldoAll()
        {
            var saldos = _saldoService.GetSaldoAll();
            return Ok(saldos);
        }

        [HttpGet("todas_paginado")]
        public IActionResult GetSaldoPaginado([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var resultado = _saldoService.GetSaldoPaginado(page, pageSize);
            return Ok(resultado);
        }
    }
}
