using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Handler;

namespace TestAuto
{
    public class InitControllerAuthTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        public InitControllerAuthTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task InicializarCuentas_ProtectedEndpoint_Returns401WithoutToken()
        {
            var client = _factory.CreateClient();
            var response = await client.PostAsync("/api/init/cuentas?cantidad=10", null);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task InicializarCuentas_ProtectedEndpoint_Returns200WithValidToken()
        {
            var client = _factory.CreateClient();
            var token = TestJwtHelper.GetValidToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.PostAsync("/api/init/cuentas?cantidad=10", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    // Helper para obtener un JWT válido para pruebas
    public static class TestJwtHelper
    {
        public static string GetValidToken()
        {
            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("SuperSecretKeyParaJWTHandler2025!"));
            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "usuario_1")
            };
            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: "HandlerAutAPI",
                audience: "HandlerAutUsers",
                claims: claims,
                expires: System.DateTime.Now.AddMinutes(30),
                signingCredentials: credentials
            );
            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
