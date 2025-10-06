using Handler.Services;
using Handler.Controllers.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Handler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InitController : ControllerBase
    {
        private readonly ICuentaInitService _initService;
        private readonly ICuentaBanksysInitService _banksysInitService;
        private readonly IHandlerStatusService _statusService;

        public InitController(ICuentaInitService initService, ICuentaBanksysInitService banksysInitService, IHandlerStatusService statusService)
        {
            _initService = initService;
            _banksysInitService = banksysInitService;
            _statusService = statusService;
        }

        //[Authorize (Roles = "admin")]
        [HttpPost("cuentas")]
        public async Task<ActionResult<InitCuentasResultDto>> InicializarCuentas([FromQuery] int cantidad = 1000)
        {
            if (!_statusService.EstaActivo())
                return StatusCode(503, "El Handler está inactivo.");
            var total = await _initService.InicializarCuentasAsync(cantidad);
            var totalBanksys = await _banksysInitService.InicializarCuentasBanksysAsync(cantidad);
            return Ok(new InitCuentasResultDto
            {
                CantidadCuentas = total,
                CantidadCuentasBanksys = totalBanksys,
                Mensaje = $"Se inicializaron {total} cuentas en el sistema y {totalBanksys} en Banksys correctamente."
            });
        }
    }

    public class InitCuentasResultDto
    {
        public int CantidadCuentas { get; set; }
        public int CantidadCuentasBanksys { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }
}
