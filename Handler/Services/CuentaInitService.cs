using Handler.Infrastructure;
using Handler.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Handler.Services
{
    /// <summary>
    /// Servicio para inicializar cuentas en la base de datos. Elimina todas las cuentas y solicitudes previas,
    /// y crea una cantidad especificada de cuentas con saldos positivos diferentes.
    /// </summary>
    public class CuentaInitService : ICuentaInitService
    {
        private readonly HandlerDbContext _db;
        public CuentaInitService(HandlerDbContext db)
        {
            _db = db;
        }

        public async Task<int> InicializarCuentasAsync(int cantidad = 1000)
        {
            /// <summary>
            /// Inicializa las cuentas en la base de datos.
            /// Elimina todos los registros de las tablas Cuentas y SolicitudesDebito,
            /// luego inserta la cantidad indicada de cuentas con saldos positivos diferentes.
            /// </summary>
            /// <param name="cantidad">Cantidad de cuentas a crear (por defecto 1000)</param>
            /// <returns>Cantidad de cuentas creadas</returns>
            await _db.Database.ExecuteSqlRawAsync("DELETE FROM SolicitudesDebito;");
            await _db.Database.ExecuteSqlRawAsync("DELETE FROM Cuentas;");

            for (int i = 1; i <= cantidad; i++)
            {
                var (numero, saldo) = CuentaFactory.GenerarCuenta(i);
                var cuenta = new Cuenta
                {
                    Numero = numero,
                    Saldo = saldo
                };
                _db.Cuentas.Add(cuenta);
            }
            await _db.SaveChangesAsync();
            return cantidad;
        }
    }
}
