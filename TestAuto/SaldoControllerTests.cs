using System.Net;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System;

namespace TestAuto
{
    [Collection("DatabaseTests")]
    public class SaldoControllerTests : IClassFixture<WebApplicationFactory<Handler.Program>>
    {
        private readonly WebApplicationFactory<Handler.Program> _factory;

        public SaldoControllerTests(WebApplicationFactory<Handler.Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetSaldo_CuentaExistente_Retorna_Ok()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // Activar el sistema
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);
            
            // Inicializar cuentas primero (como en InitControllerTests)
            var initResponse = await client.PostAsync("/api/init/cuentas", null);
            Assert.Equal(HttpStatusCode.OK, initResponse.StatusCode);
            
            // Esperar un momento para que la inicialización se complete
            await Task.Delay(100);
            
            // Asegurar que existan cuentas (las cuentas deberían existir del test de inicialización previo)
            long numeroCuenta = 1000000001;

            // Act
            var response = await client.GetAsync($"/api/saldo/cuenta/{numeroCuenta}");

            // Debug: si hay error, mostrar el contenido
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error en consulta de saldo para cuenta {numeroCuenta}: {response.StatusCode} - {errorContent}");
            }

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
            
            // Verificar que el JSON contiene el saldo
            Assert.Contains("saldo", content.ToLower());
        }

        [Fact]
        public async Task GetSaldo_CuentaInexistente_Retorna_NotFound()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // Activar el sistema
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);
            
            // Usar un número de cuenta que no debería existir
            long numeroCuentaInexistente = 9999999999;

            // Act
            var response = await client.GetAsync($"/api/saldo/cuenta/{numeroCuentaInexistente}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetSaldo_SistemaInactivo_Retorna_ServiceUnavailable()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // Inactivar el sistema
            var inactivarResponse = await client.PostAsync("/api/status/inactivar", null);
            Assert.Equal(HttpStatusCode.OK, inactivarResponse.StatusCode);
            
            long numeroCuenta = 1000000001;

            // Act
            var response = await client.GetAsync($"/api/saldo/cuenta/{numeroCuenta}");

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            
            // Reactivar el sistema para no afectar otros tests
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);
        }

        [Fact]
        public async Task GetSaldo_ParametroInvalido_Retorna_BadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // Activar el sistema
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);

            // Act - Usar un parámetro inválido (texto en lugar de número)
            var response = await client.GetAsync("/api/saldo/cuenta/invalid_account");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetSaldo_VerificarFormatoRespuesta()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // Activar el sistema
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);
            
            long numeroCuenta = 1000000001;

            // Act
            var response = await client.GetAsync($"/api/saldo/cuenta/{numeroCuenta}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            // Verificar Content-Type
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            
            var content = await response.Content.ReadAsStringAsync();
            
            // Verificar que es JSON válido y contiene campos esperados
            Assert.True(content.StartsWith("{") && content.EndsWith("}"));
            Assert.Contains("saldo", content.ToLower());
            Assert.Contains("numero", content.ToLower());
        }

        [Fact]
        public async Task GetSaldoAll_Retorna_Ok()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // Activar el sistema
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);

            // Act
            var response = await client.GetAsync("/api/saldo/todas");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
            
            // Verificar que es un array JSON
            Assert.True(content.StartsWith("[") && content.EndsWith("]"));
        }

        [Fact]
        public async Task GetSaldoPaginado_SinParametros_Retorna_Ok()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // Activar el sistema
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);

            // Act
            var response = await client.GetAsync("/api/saldo/todas_paginado");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
            
            // Verificar que contiene información de paginación
            Assert.Contains("totalCuentas", content);
            Assert.Contains("paginaActual", content);
        }

        [Fact]
        public async Task GetSaldoPaginado_ConParametros_Retorna_Ok()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // Activar el sistema
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);

            // Act
            var response = await client.GetAsync("/api/saldo/todas_paginado?page=1&pageSize=5");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
            
            // Verificar formato de respuesta paginada
            Assert.Contains("totalCuentas", content);
            Assert.Contains("paginaActual", content);
            Assert.Contains("cuentas", content); // En lugar de tamañoPagina, buscar el array de cuentas
        }

        [Fact]
        public async Task GetSaldoAll_SistemaInactivo_Retorna_ServiceUnavailable()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // Inactivar el sistema
            var inactivarResponse = await client.PostAsync("/api/status/inactivar", null);
            Assert.Equal(HttpStatusCode.OK, inactivarResponse.StatusCode);

            // Act
            var response = await client.GetAsync("/api/saldo/todas");

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            
            // Reactivar el sistema para no afectar otros tests
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);
        }
    }
}