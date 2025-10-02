using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Handler.Services;

namespace Handler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Seguimiento de login: usuario y valores de configuración
            Console.WriteLine($"Intento de login para usuario: {request.Username}");
            var jwtConfig = _authService.GetJwtConfigValues();
            Console.WriteLine($"JwtConfig.SecretKey: {jwtConfig.SecretKey}");
            Console.WriteLine($"JwtConfig.Issuer: {jwtConfig.Issuer}");
            Console.WriteLine($"JwtConfig.Audience: {jwtConfig.Audience}");

            var token = _authService.Authenticate(request.Username, request.Password);
            if (token != null)
            {
                Console.WriteLine($"Token generado correctamente para usuario: {request.Username}");
                return Ok(new { token });
            }
            Console.WriteLine($"Login fallido para usuario: {request.Username}");
            return Unauthorized();
        }
    }

    public class LoginRequest
    {
    public required string Username { get; set; }
    public required string Password { get; set; }
    }
}
