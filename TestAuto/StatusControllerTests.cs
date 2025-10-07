
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TestAuto
{
    public class StatusControllerTests : IClassFixture<WebApplicationFactory<Handler.Program>>
    {
        private readonly WebApplicationFactory<Handler.Program> _factory;

        public StatusControllerTests(WebApplicationFactory<Handler.Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task HealthEndpoint_Retorna_Ok()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/status/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ActivarEndpoint_Retorna_Ok()
        {
            var client = _factory.CreateClient();
            var response = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task InactivarEndpoint_Retorna_Ok()
        {
            var client = _factory.CreateClient();
            var response = await client.PostAsync("/api/status/inactivar", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Reactivar el sistema al final para no afectar otras pruebas
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);
        }
    }
}

