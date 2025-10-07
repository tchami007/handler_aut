using System.Net;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TestAuto
{
    [Collection("DatabaseTests")]
    public class InitControllerTests : IClassFixture<WebApplicationFactory<Handler.Program>>
    {
        private readonly WebApplicationFactory<Handler.Program> _factory;

        public InitControllerTests(WebApplicationFactory<Handler.Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task InitEndpoint_Retorna_Ok()
        {
            var client = _factory.CreateClient();
            
            // Activar el sistema antes de inicializar
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);
            
            // Enviar el parámetro cantidad por query string
            var response = await client.PostAsync("/api/Init/cuentas?cantidad=1000", null);
            
            // Debug: si hay error, mostrar el contenido
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error en inicialización: {response.StatusCode} - {errorContent}");
            }
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
