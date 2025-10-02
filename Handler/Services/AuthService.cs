using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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

        public AuthService(IConfiguration config)
        {
            _config = config;
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
                Console.WriteLine($"[AuthService] JwtConfig.SecretKey: {jwtConfig["SecretKey"]}");
                Console.WriteLine($"[AuthService] JwtConfig.Issuer: {jwtConfig["Issuer"]}");
                Console.WriteLine($"[AuthService] JwtConfig.Audience: {jwtConfig["Audience"]}");

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
                Console.WriteLine($"[AuthService] Token generado para usuario: {usuario.Username}");
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
