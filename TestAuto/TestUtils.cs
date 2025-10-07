using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Handler.Controllers.Dtos;

namespace TestAuto
{
    public static class TestUtils
    {
        public static async Task<decimal> ObtenerSaldo(HttpClient client, long numeroCuenta)
        {
            // Asume endpoint: /api/saldo/{numeroCuenta} que retorna { saldo: decimal }
    var response = await client.GetAsync($"/api/saldo/cuenta/{numeroCuenta}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<SaldoResponseDto>();
            return result?.Saldo ?? 0m;
        }

        public static async Task CrearCuentaConSaldo(HttpClient client, long numeroCuenta, decimal saldo)
        {
            // Si existe endpoint para crear cuenta, usarlo. Si no, simular con primer movimiento de crédito.
            var dto = new Handler.Controllers.Dtos.RegistroSolicitudDto
            {
                NumeroCuenta = numeroCuenta,
                Monto = saldo,
                TipoMovimiento = "credito",
                NumeroComprobante = 99999
            };
            var response = await client.PostAsJsonAsync("/api/solicitud", dto);
            response.EnsureSuccessStatusCode();
        }

        private class SaldoResponseDto
        {
            public decimal Saldo { get; set; }
        }
    }
}
