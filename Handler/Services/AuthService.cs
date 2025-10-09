using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Handler.Services
{
    public interface IAuthService
    {
    string? Authenticate(string username, string password);
    JwtConfigValues GetJwtConfigValues();
    }

    public class AuthService : IAuthService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IConfiguration config, ILogger<AuthService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public JwtConfigValues GetJwtConfigValues()
        {
            var jwtConfig = _config.GetSection("Jwt");
            return new JwtConfigValues
            {
                SecretKey = jwtConfig["SecretKey"],
                Issuer = jwtConfig["Issuer"],
                Audience = jwtConfig["Audience"]
            };
        }

        public string? Authenticate(string username, string password)
        {
            // Prototipo: usuario/clave fijos
            var usuarios = new List<(string Username, string Password, List<string> Roles)>
            {
                ("admin", "admin123", new List<string> { "admin" }),
                ("usuario_1", "clave1", new List<string> { "user" }),
                ("usuario_2", "clave2", new List<string> { "user" }),
                ("test", "1234", new List<string> { "user" })
            };

            var usuario = usuarios.FirstOrDefault(u => u.Username == username && u.Password == password);
            if (usuario.Username != null)
            {
                var jwtConfig = _config.GetSection("Jwt");
                // Seguimiento de valores recuperados
                _logger.LogDebug("JwtConfig.SecretKey: {SecretKey}", jwtConfig["SecretKey"]);
                _logger.LogDebug("JwtConfig.Issuer: {Issuer}", jwtConfig["Issuer"]);
                _logger.LogDebug("JwtConfig.Audience: {Audience}", jwtConfig["Audience"]);

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, usuario.Username)
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["SecretKey"] ?? ""));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: jwtConfig["Issuer"],
                    audience: jwtConfig["Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddHours(1),
                    signingCredentials: creds
                );
                _logger.LogInformation("Token generado para usuario: {Username}", usuario.Username);
                return new JwtSecurityTokenHandler().WriteToken(token);
            }

            // Usuario no válido
            return null;
        }
    }


    /// <summary>
    /// Valores de configuración para JWT recuperados desde appsettings.
    /// </summary>
    public class JwtConfigValues
    {
        public string? SecretKey { get; set; }
        public string? Issuer { get; set; }
        public string? Audience { get; set; }
    }
}
