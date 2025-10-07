using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Handler.Controllers.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
namespace TestAuto
{
    public class SolicitudSaldoFinalTests : IClassFixture<WebApplicationFactory<Handler.Program>>
    {
        private readonly HttpClient _client;

        public SolicitudSaldoFinalTests(WebApplicationFactory<Handler.Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task MovimientosParalelos_CalculaSaldoFinalCorrecto()
        {

            long numeroCuenta = 1000000002;
            decimal saldoInicial = 0m;
            try
            {
                saldoInicial = await TestUtils.ObtenerSaldo(_client, numeroCuenta);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("404"))
            {
                // Si la cuenta no existe, crearla con saldo 0
                await TestUtils.CrearCuentaConSaldo(_client, numeroCuenta, 0m);
                saldoInicial = 0m;
            }

            var movimientos = new List<(string tipo, decimal monto)>
            {
                ("debito", 200),   // saldo: -200
                ("credito", 100),  // saldo: +100
                ("debito", 50),    // saldo: -50
                ("credito", 400),  // saldo: +400
                ("debito", 300)    // saldo: -300
            };
            decimal saldoEsperado = saldoInicial;
            
            // Generar números de comprobante únicos usando timestamp en segundos + índice + random
            int baseTimestamp = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 1000000); // Limitar a 6 dígitos
            var random = new Random();

            // Disparar movimientos en paralelo
            var tasks = new List<Task>();
            for (int i = 0; i < movimientos.Count; i++)
            {
                var mov = movimientos[i];
                // Generar comprobante único: timestamp + índice*1000 + número aleatorio (máximo 9 dígitos)
                long comprobante = baseTimestamp + (i * 1000) + random.Next(100, 999);
                tasks.Add(Task.Run(async () =>
                {
                    var dto = new RegistroSolicitudDto
                    {
                        NumeroCuenta = numeroCuenta,
                        Monto = mov.monto,
                        TipoMovimiento = mov.tipo,
                        NumeroComprobante = comprobante
                    };
                    
                    // Reintentar en caso de conflictos de concurrencia (400/409)
                    int reintentos = 10;
                    while (reintentos-- > 0)
                    {
                        var response = await _client.PostAsJsonAsync("/api/solicitud", dto);
                        if (response.IsSuccessStatusCode)
                        {
                            break; // Éxito, salir del bucle
                        }
                        
                        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        {
                            // Posible conflicto de concurrencia, esperar y reintentar
                            await Task.Delay(new Random().Next(50, 100));
                            if (reintentos == 0)
                            {
                                response.EnsureSuccessStatusCode(); // Lanzar excepción en último intento
                            }
                        }
                        else
                        {
                            response.EnsureSuccessStatusCode(); // Otros errores, fallar inmediatamente
                        }
                    }
                }));
                // Calcular saldo esperado localmente (secuencial, para comparar)
                if (mov.tipo == "debito") saldoEsperado -= mov.monto;
                if (mov.tipo == "credito") saldoEsperado += mov.monto;
            }
            await Task.WhenAll(tasks);

            // Act
            var getResponse = await _client.GetAsync($"/api/solicitud/cuenta/{numeroCuenta}");
            getResponse.EnsureSuccessStatusCode();
            var result = await getResponse.Content.ReadFromJsonAsync<List<SolicitudDebitoDto>>();

            // Assert
            Assert.NotNull(result);
            var ultimo = result.Count > 0 ? result[^1] : null;
            Assert.NotNull(ultimo);
            Assert.Equal(saldoEsperado, ultimo.SaldoRespuesta);
        }

        [Fact]
        public async Task SecuenciaDebitosCreditos_CalculaSaldoFinalCorrecto()
        {
            // Arrange

            long numeroCuenta = 1000000001;
            decimal saldoInicial = 0m;
            try
            {
                saldoInicial = await TestUtils.ObtenerSaldo(_client, numeroCuenta);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("404"))
            {
                // Si la cuenta no existe, crearla con saldo 0
                await TestUtils.CrearCuentaConSaldo(_client, numeroCuenta, 0m);
                saldoInicial = 0m;
            }

            // Secuencia de movimientos: (tipo, monto)
            var movimientos = new List<(string tipo, decimal monto)>
            {
                ("debito", 100),   // saldo: -100
                ("credito", 50),   // saldo: +50
                ("debito", 200),   // saldo: -200
                ("credito", 300),  // saldo: +300
                ("debito", 150)    // saldo: -150
            };
            decimal saldoEsperado = saldoInicial;
            int comprobanteBase = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            for (int i = 0; i < movimientos.Count; i++)
            {
                var mov = movimientos[i];
                var dto = new RegistroSolicitudDto
                {
                    NumeroCuenta = numeroCuenta,
                    Monto = mov.monto,
                    TipoMovimiento = mov.tipo,
                    NumeroComprobante = comprobanteBase + i // siempre único
                };
                var response = await _client.PostAsJsonAsync("/api/solicitud", dto);
                response.EnsureSuccessStatusCode();
                // Calcular saldo esperado localmente
                if (mov.tipo == "debito") saldoEsperado -= mov.monto;
                if (mov.tipo == "credito") saldoEsperado += mov.monto;
            }

            // Act
            var getResponse = await _client.GetAsync($"/api/solicitud/cuenta/{numeroCuenta}");
            getResponse.EnsureSuccessStatusCode();
            var result = await getResponse.Content.ReadFromJsonAsync<List<SolicitudDebitoDto>>();

            // Assert
            Assert.NotNull(result);
            var ultimo = result.Count > 0 ? result[^1] : null;
            Assert.NotNull(ultimo);
            Assert.Equal(saldoEsperado, ultimo.SaldoRespuesta);
        }
    }

}
