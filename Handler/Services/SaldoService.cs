using Handler.Controllers.Dtos;
using Handler.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Handler.Services
{
    public class SaldoService : ISaldoService
    {
        private readonly HandlerDbContext _db;
        public SaldoService(HandlerDbContext db)
        {
            _db = db;
        }

        public SaldoCuentaDto? GetSaldoByCuenta(long numeroCuenta)
        {
            var cuenta = _db.Cuentas.FirstOrDefault(c => c.Numero == numeroCuenta);
            if (cuenta == null) return null;
            return new SaldoCuentaDto { NumeroCuenta = cuenta.Numero, Saldo = cuenta.Saldo };
        }

        public List<SaldoCuentaDto> GetSaldoAll()
        {
            return _db.Cuentas.Select(c => new SaldoCuentaDto { NumeroCuenta = c.Numero, Saldo = c.Saldo }).ToList();
        }

        public SaldoCuentaPaginadoDto GetSaldoPaginado(int page, int pageSize)
        {
            var totalCuentas = _db.Cuentas.Count();
            var totalPaginas = (int)System.Math.Ceiling((double)totalCuentas / pageSize);
            var cuentas = _db.Cuentas
                .OrderBy(c => c.Numero)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new SaldoCuentaDto { NumeroCuenta = c.Numero, Saldo = c.Saldo })
                .ToList();
            return new SaldoCuentaPaginadoDto
            {
                TotalCuentas = totalCuentas,
                PaginaActual = page,
                TotalPaginas = totalPaginas,
                PageSize = pageSize,
                Cuentas = cuentas
            };
        }

    }
}
