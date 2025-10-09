using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Handler.Controllers.Dtos;
using System;

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


        // Verifica que el endpoint GET /api/config/colas retorna 200 OK y la estructura esperada cuando el sistema está activo.
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

        // Verifica que el endpoint GET /api/config/colas retorna 503 Service Unavailable cuando el sistema está inactivo.
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

        // Verifica que el endpoint GET /api/config/colas retorna la estructura correcta del JSON
        [Fact]
        public async Task GetColas_VerificarEstructuraRespuesta()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // Activar el sistema y esperar un momento
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);
            await Task.Delay(50); // Pequeña espera para asegurar activación

            // Agregar al menos una cola para tener contenido válido
            var nuevaCola = new { nombre = "cola_test_estructura_" + DateTime.Now.Ticks };
            var jsonContent = new StringContent(JsonSerializer.Serialize(nuevaCola), Encoding.UTF8, "application/json");
            var agregarResponse = await client.PostAsync("/api/config/colas/agregar", jsonContent);
            Assert.Equal(HttpStatusCode.OK, agregarResponse.StatusCode);

            // Act
            var response = await client.GetAsync("/api/config/colas");
            
            // Debug: si hay error, mostrar información
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error en GetColas: {response.StatusCode} - {errorContent}");
            }

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

        // Verifica que el endpoint POST /api/config/colas/agregar agrega una nueva cola correctamente cuando el sistema está activo.
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

        // Verifica que el endpoint POST /api/config/colas/agregar retorna 503 Service Unavailable cuando el sistema está inactivo.
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

        // Verifica que el nombre de la cola agregada sigue un patrón incremental (cola_N)
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

        // Verifica que el endpoint DELETE /api/config/colas/ultima retorna 200 OK cuando el sistema está activo.
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

        // Verifica que el endpoint DELETE /api/config/colas/ultima retorna 503 Service Unavailable cuando el sistema está inactivo.
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

        // Verifica que no se pueden agregar más de 10 colas (límite basado en archivos de configuración del Worker)
        [Fact]
        public async Task AgregarCola_LimiteMaximo_Retorna_BadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // Activar el sistema
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);

            // Obtener configuración actual
            var configResponse = await client.GetAsync("/api/config/colas");
            var configContent = await configResponse.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var config = JsonSerializer.Deserialize<RabbitConfigDto>(configContent, options);
            
            // Agregar colas hasta llegar al límite de 10
            while (config?.Colas.Count < 10)
            {
                var agregarResponse = await client.PostAsync("/api/config/colas/agregar", null);
                Assert.Equal(HttpStatusCode.OK, agregarResponse.StatusCode);
                
                configResponse = await client.GetAsync("/api/config/colas");
                configContent = await configResponse.Content.ReadAsStringAsync();
                config = JsonSerializer.Deserialize<RabbitConfigDto>(configContent, options);
            }

            // Verificar que tenemos exactamente 10 colas
            Assert.Equal(10, config?.Colas.Count);

            // Act - Intentar agregar una cola más (la 11va)
            var response = await client.PostAsync("/api/config/colas/agregar", null);

            // Assert - Debe fallar con BadRequest
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            
            var errorContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Límite máximo: 10", errorContent);
        }
    }
}