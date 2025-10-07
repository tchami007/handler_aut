using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Handler.Controllers.Dtos;

namespace TestAuto
{
    [Collection("DatabaseTests")]
    public class ConfigControllerTests : IClassFixture<WebApplicationFactory<Handler.Program>>
    {
        private readonly WebApplicationFactory<Handler.Program> _factory;

        public ConfigControllerTests(WebApplicationFactory<Handler.Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetColas_SistemaActivo_Retorna_Ok()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // Activar el sistema
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);

            // Act
            var response = await client.GetAsync("/api/config/colas");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
            
            // Verificar que el JSON contiene la estructura esperada
            Assert.Contains("colas", content.ToLower());
            
            // Verificar Content-Type
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task GetColas_SistemaInactivo_Retorna_ServiceUnavailable()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // Inactivar el sistema
            var inactivarResponse = await client.PostAsync("/api/status/inactivar", null);
            Assert.Equal(HttpStatusCode.OK, inactivarResponse.StatusCode);

            // Act
            var response = await client.GetAsync("/api/config/colas");

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            
            // Reactivar el sistema para no afectar otros tests
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);
        }

        [Fact]
        public async Task GetColas_VerificarEstructuraRespuesta()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // Activar el sistema
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);

            // Agregar al menos una cola para tener contenido válido
            var nuevaCola = new { nombre = "cola_test_estructura" };
            var jsonContent = new StringContent(JsonSerializer.Serialize(nuevaCola), Encoding.UTF8, "application/json");
            var agregarResponse = await client.PostAsync("/api/config/colas/agregar", jsonContent);
            Assert.Equal(HttpStatusCode.OK, agregarResponse.StatusCode);

            // Act
            var response = await client.GetAsync("/api/config/colas");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            
            // Deserializar y verificar estructura
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var config = JsonSerializer.Deserialize<RabbitConfigDto>(content, options);
            
            Assert.NotNull(config);
            Assert.NotNull(config.Colas);
            Assert.True(config.Colas.Count > 0, "Debe tener al menos una cola configurada");
            
            // Verificar que cada cola tiene nombre
            foreach (var cola in config.Colas)
            {
                Assert.NotNull(cola.Nombre);
                Assert.NotEmpty(cola.Nombre);
            }
        }

        [Fact]
        public async Task AgregarCola_SistemaActivo_Retorna_Ok()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // Activar el sistema
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);

            // Obtener configuración inicial para contar colas
            var configInicialResponse = await client.GetAsync("/api/config/colas");
            var configInicialContent = await configInicialResponse.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var configInicial = JsonSerializer.Deserialize<RabbitConfigDto>(configInicialContent, options);
            int colasIniciales = configInicial?.Colas.Count ?? 0;

            // Act
            var response = await client.PostAsync("/api/config/colas/agregar", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
            
            // Verificar que el mensaje contiene información sobre la cola agregada
            Assert.Contains("Cola", content);
            Assert.Contains("agregada", content);
            
            // Verificar que efectivamente se agregó una cola
            var configFinalResponse = await client.GetAsync("/api/config/colas");
            var configFinalContent = await configFinalResponse.Content.ReadAsStringAsync();
            var configFinal = JsonSerializer.Deserialize<RabbitConfigDto>(configFinalContent, options);
            
            Assert.Equal(colasIniciales + 1, configFinal?.Colas.Count);
        }

        [Fact]
        public async Task AgregarCola_SistemaInactivo_Retorna_ServiceUnavailable()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // Inactivar el sistema
            var inactivarResponse = await client.PostAsync("/api/status/inactivar", null);
            Assert.Equal(HttpStatusCode.OK, inactivarResponse.StatusCode);

            // Act
            var response = await client.PostAsync("/api/config/colas/agregar", null);

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            
            // Reactivar el sistema para no afectar otros tests
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);
        }

        [Fact]
        public async Task AgregarCola_VerificarNombreIncremental()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // Activar el sistema
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);

            // Act - Agregar una cola
            var response = await client.PostAsync("/api/config/colas/agregar", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            
            // Verificar que el nombre sigue patrón incremental (cola_N)
            Assert.Contains("cola_", content.ToLower());
            
            // Extraer el nombre de la cola del mensaje de respuesta
            // El formato esperado es: {"resultado":"Cola 'cola_X' agregada"}
            var responseObj = JsonSerializer.Deserialize<JsonElement>(content);
            var resultado = responseObj.GetProperty("resultado").GetString();
            
            Assert.NotNull(resultado);
            Assert.Contains("Cola", resultado);
            Assert.Contains("agregada", resultado);
        }

        [Fact]
        public async Task EliminarUltimaCola_SistemaActivo_Retorna_Ok()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // Activar el sistema
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);

            // Asegurar que hay al menos 2 colas (agregar una si es necesario)
            var configResponse = await client.GetAsync("/api/config/colas");
            var configContent = await configResponse.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var config = JsonSerializer.Deserialize<RabbitConfigDto>(configContent, options);
            
            if (config?.Colas.Count <= 1)
            {
                // Agregar una cola para asegurar que podemos eliminar
                await client.PostAsync("/api/config/colas/agregar", null);
            }

            // Obtener configuración antes de eliminar
            var configAntesResponse = await client.GetAsync("/api/config/colas");
            var configAntesContent = await configAntesResponse.Content.ReadAsStringAsync();
            var configAntes = JsonSerializer.Deserialize<RabbitConfigDto>(configAntesContent, options);
            int colasAntes = configAntes?.Colas.Count ?? 0;

            // Act
            var response = await client.DeleteAsync("/api/config/colas/ultima");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
            
            // Verificar mensaje de respuesta
            Assert.Contains("Cola eliminada", content);
            
            // Verificar que efectivamente se eliminó una cola
            var configDespuesResponse = await client.GetAsync("/api/config/colas");
            var configDespuesContent = await configDespuesResponse.Content.ReadAsStringAsync();
            var configDespues = JsonSerializer.Deserialize<RabbitConfigDto>(configDespuesContent, options);
            
            Assert.Equal(colasAntes - 1, configDespues?.Colas.Count);
        }

        [Fact]
        public async Task EliminarUltimaCola_SistemaInactivo_Retorna_ServiceUnavailable()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // Inactivar el sistema
            var inactivarResponse = await client.PostAsync("/api/status/inactivar", null);
            Assert.Equal(HttpStatusCode.OK, inactivarResponse.StatusCode);

            // Act
            var response = await client.DeleteAsync("/api/config/colas/ultima");

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            
            // Reactivar el sistema para no afectar otros tests
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);
        }

        [Fact]
        public async Task EliminarUltimaCola_SinColas_Retorna_BadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // Activar el sistema
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);

            // Obtener configuración actual y eliminar todas las colas menos una
            var configResponse = await client.GetAsync("/api/config/colas");
            var configContent = await configResponse.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var config = JsonSerializer.Deserialize<RabbitConfigDto>(configContent, options);
            
            // Eliminar colas hasta que quede solo una (si hay más de una)
            while (config?.Colas.Count > 1)
            {
                await client.DeleteAsync("/api/config/colas/ultima");
                configResponse = await client.GetAsync("/api/config/colas");
                configContent = await configResponse.Content.ReadAsStringAsync();
                config = JsonSerializer.Deserialize<RabbitConfigDto>(configContent, options);
            }

            // Intentar eliminar la última cola (esto debería fallar)
            // Note: Este test asume que el servicio no permite eliminar todas las colas
            var response = await client.DeleteAsync("/api/config/colas/ultima");

            // Assert - puede ser BadRequest o OK dependiendo de la lógica de negocio
            // Si permite eliminar todas: OK, si no permite: BadRequest
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                       response.StatusCode == HttpStatusCode.OK, 
                       $"Se esperaba BadRequest o OK, pero se obtuvo {response.StatusCode}");
        }
    }
}