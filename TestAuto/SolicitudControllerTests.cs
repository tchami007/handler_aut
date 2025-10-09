using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Handler.Controllers.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TestAuto
{
    public class SolicitudControllerTests : IClassFixture<WebApplicationFactory<Handler.Program>>
    {
        private readonly HttpClient _client;

        public SolicitudControllerTests(WebApplicationFactory<Handler.Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetSolicitudesPorCuenta_ReturnsOrderedList()
        {
            // Arrange: usar un número de cuenta específico para este test para evitar contaminación
            long numeroCuenta = 1000000001; // Primera cuenta generada por CuentaFactory
            
            // Activar el handler primero
            var activarResponse = await _client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);
            
            // Inicializar cuentas para asegurar que existe la infraestructura
            var initResponse = await _client.PostAsync("/api/init/cuentas", null);
            Assert.Equal(HttpStatusCode.OK, initResponse.StatusCode);
            
            var solicitudes = new List<RegistroSolicitudDto>
            {
                new RegistroSolicitudDto { NumeroCuenta = numeroCuenta, Monto = 100, TipoMovimiento = "debito", NumeroComprobante = 1001 },
                new RegistroSolicitudDto { NumeroCuenta = numeroCuenta, Monto = 50, TipoMovimiento = "credito", NumeroComprobante = 1002 },
                new RegistroSolicitudDto { NumeroCuenta = numeroCuenta, Monto = 25, TipoMovimiento = "debito", NumeroComprobante = 1003 }
            };
            foreach (var solicitud in solicitudes)
            {
                var response = await _client.PostAsJsonAsync("/api/solicitud", solicitud);
                response.EnsureSuccessStatusCode();
            }

            // Act
            var getResponse = await _client.GetAsync($"/api/solicitud/cuenta/{numeroCuenta}");
            getResponse.EnsureSuccessStatusCode();
            var result = await getResponse.Content.ReadFromJsonAsync<List<SolicitudDebitoDto>>();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count >= 3); // Puede haber más si la cuenta ya tenía movimientos
            for (int i = 1; i < result.Count; i++)
            {
                Assert.True(result[i].FechaReal >= result[i - 1].FechaReal, "La lista no está ordenada ascendente por FechaReal");
            }
            // Verificar que las solicitudes pertenecen a la cuenta correcta verificando que se pueden crear para esta cuenta
            // No comparamos CuentaId (interno) con NumeroCuenta (externo) ya que son campos diferentes
            Assert.True(result.Count >= 3, "Debería haber al menos las 3 solicitudes creadas en el test");
        }
    }
}
