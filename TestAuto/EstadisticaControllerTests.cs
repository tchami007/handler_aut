using System.Net;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TestAuto
{
    public class EstadisticaControllerTests : IClassFixture<WebApplicationFactory<Handler.Program>>
    {
        private readonly WebApplicationFactory<Handler.Program> _factory;

        public EstadisticaControllerTests(WebApplicationFactory<Handler.Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task EstadisticaEndpoint_Retorna_Ok()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/estadistica");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadAsStringAsync();
            Assert.Contains("SolicitudesProcesadas", json);
        }

        [Theory]
        [InlineData("debito")]
        [InlineData("credito")]
        [InlineData("contrasiento_debito")]
        [InlineData("contrasiento_credito")]
        public async Task EstadisticaEndpoint_FiltraPorTipoMovimiento(string tipo)
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync($"/api/estadistica?tipoMovimiento={tipo}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadAsStringAsync();
            Assert.Contains("SolicitudesProcesadas", json);
        }
    }
}
