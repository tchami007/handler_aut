/*
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Collections.Generic;
using System.Net.Http.Json;

namespace TestAuto
{
    [Collection("DatabaseTests")]
    public class IntegracionColasTests : IClassFixture<WebApplicationFactory<Handler.Program>>
    {
        private readonly WebApplicationFactory<Handler.Program> _factory;

        public IntegracionColasTests(WebApplicationFactory<Handler.Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Solicitudes_Se_Distribuyen_En_Todas_Las_Colas()
        {
            // AVISO: Antes de correr este test, asegúrate de que el Handler y todos los Workers estén ejecutándose.
            var client = _factory.CreateClient();

            // Activar el sistema antes de enviar solicitudes
            var activarResponse = await client.PostAsync("/api/status/activar", null);
            Assert.Equal(HttpStatusCode.OK, activarResponse.StatusCode);

            // Las cuentas deberían existir por el test de inicialización previo
            // Si no existen, el test fallará con un mensaje claro

            int cantidadColas = 7; // Ajusta según la configuración actual
            var cuentas = new List<string> { "1000000001", "1000000002", "1000000003", "1000000004", "1000000005", "1000000006", "1000000007" };
            var colasRecibidas = new HashSet<string>();

            foreach (var cuenta in cuentas)
            {
                var request = new {
                    NumeroCuenta = long.Parse(cuenta),
                    Monto = 100.00m,
                    TipoMovimiento = "debito",
                    MovimientoOriginalId = (int?)null,
                    NumeroComprobante = 1000000000L + long.Parse(cuenta.Substring(cuenta.Length - 3))
                };
                var response = await client.PostAsJsonAsync("/api/solicitud", request);
                
                // Debug: si hay error, mostrar el contenido para entender qué falla
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error en solicitud para cuenta {cuenta}: {response.StatusCode} - {errorContent}");
                }
                
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var json = await response.Content.ReadAsStringAsync();
                // Suponiendo que la respuesta indica la cola utilizada
                // Extrae el nombre de la cola de la respuesta (ajusta según la API real)
                var cola = ExtraerColaDeRespuesta(json);
                colasRecibidas.Add(cola);
            }

            Assert.Equal(cantidadColas, colasRecibidas.Count);
        }

        private string ExtraerColaDeRespuesta(string json)
        {
            // Implementa la lógica para extraer el nombre de la cola desde el JSON de respuesta
            // Por ejemplo, si la respuesta es: { "cola": "cola_1", ... }
            var match = System.Text.RegularExpressions.Regex.Match(json, @"""cola""\s*:\s*""(cola_\d+)""");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }
    }
}
*/