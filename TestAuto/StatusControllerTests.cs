using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Handler;

namespace TestAuto
{
    public class StatusControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        public StatusControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Health_ReturnsOk()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/status/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Activar_ReturnsOk_WithValidToken()
        {
            var client = _factory.CreateClient();
            var token = TestJwtHelper.GetValidToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Inactivar_ReturnsOk_WithValidToken()
        {
            var client = _factory.CreateClient();
            var token = TestJwtHelper.GetValidToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.PostAsync("/api/status/inactivar", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Activar_ReturnsUnauthorized_WithoutToken()
        {
            var client = _factory.CreateClient();
            var response = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Inactivar_ReturnsUnauthorized_WithoutToken()
        {
            var client = _factory.CreateClient();
            var response = await client.PostAsync("/api/status/inactivar", null);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

    }
}
