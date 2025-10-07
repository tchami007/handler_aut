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
            // Arrange: crear una cuenta y registrar varias solicitudes
            long numeroCuenta = 12345678901;
            var solicitudes = new List<RegistroSolicitudDto>
            {
                new RegistroSolicitudDto { NumeroCuenta = numeroCuenta, Monto = 100, TipoMovimiento = "debito", NumeroComprobante = 1 },
                new RegistroSolicitudDto { NumeroCuenta = numeroCuenta, Monto = 50, TipoMovimiento = "credito", NumeroComprobante = 2 },
                new RegistroSolicitudDto { NumeroCuenta = numeroCuenta, Monto = 25, TipoMovimiento = "debito", NumeroComprobante = 3 }
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
            Assert.All(result, r => Assert.Equal(numeroCuenta, r.CuentaId == 0 ? 0 : r.CuentaId));
        }
    }
}
