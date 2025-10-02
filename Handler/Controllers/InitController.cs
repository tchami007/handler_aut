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
        public InitController(ICuentaInitService initService, ICuentaBanksysInitService banksysInitService)
        {
            _initService = initService;
            _banksysInitService = banksysInitService;
        }

        //[Authorize (Roles = "admin")]
        [HttpPost("cuentas")]
        public async Task<ActionResult<InitCuentasResultDto>> InicializarCuentas([FromQuery] int cantidad = 1000)
            {
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
        public string Mensaje { get; set; }
    }
}
