using Microsoft.Data.SqlClient;

namespace Handler.Services
{
    public interface ICuentaBanksysInitService
    {
        Task<int> InicializarCuentasBanksysAsync(int cantidad = 1000);
    }

    public class CuentaBanksysInitService : ICuentaBanksysInitService
    {
        private readonly IConfiguration _configuration;
        public CuentaBanksysInitService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<int> InicializarCuentasBanksysAsync(int cantidad = 1000)
        {
            var connStr = _configuration.GetConnectionString("BanksysConnection");
            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();
            using var deleteCmd = conn.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM Cuentas;";
            await deleteCmd.ExecuteNonQueryAsync();
            int count = 0;
            for (int i = 1; i <= cantidad; i++)
            {
                var (numero, saldo) = CuentaFactory.GenerarCuenta(i);
                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = "INSERT INTO Cuentas (NumeroCuenta, Saldo) VALUES (@NumeroCuenta, @Saldo)";
                insertCmd.Parameters.AddWithValue("@NumeroCuenta", numero);
                insertCmd.Parameters.AddWithValue("@Saldo", saldo);
                await insertCmd.ExecuteNonQueryAsync();
                count++;
            }
            return count;
        }
    }
}
