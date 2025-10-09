using Handler.Controllers.Dtos;
using Handler.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Handler.Services
{
    public class SaldoService : ISaldoService
    {
        private readonly ICuentaRepository _cuentaRepository;
        
        public SaldoService(ICuentaRepository cuentaRepository)
        {
            _cuentaRepository = cuentaRepository;
        }

        public SaldoCuentaDto? GetSaldoByCuenta(long numeroCuenta)
        {
            var cuenta = _cuentaRepository.GetByNumeroAsync(numeroCuenta).Result;
            if (cuenta == null) return null;
            return new SaldoCuentaDto { NumeroCuenta = cuenta.Numero, Saldo = cuenta.Saldo };
        }

        public List<SaldoCuentaDto> GetSaldoAll()
        {
            var cuentas = _cuentaRepository.GetAllAsync().Result;
            return cuentas.Select(c => new SaldoCuentaDto { NumeroCuenta = c.Numero, Saldo = c.Saldo }).ToList();
        }

        public SaldoCuentaPaginadoDto GetSaldoPaginado(int page, int pageSize)
        {
            var totalCuentas = _cuentaRepository.GetTotalCountAsync().Result;
            var totalPaginas = (int)System.Math.Ceiling((double)totalCuentas / pageSize);
            var cuentas = _cuentaRepository.GetPaginatedAsync((page - 1) * pageSize, pageSize).Result;
            
            var saldosDto = cuentas.Select(c => new SaldoCuentaDto { NumeroCuenta = c.Numero, Saldo = c.Saldo }).ToList();
            
            return new SaldoCuentaPaginadoDto
            {
                TotalCuentas = totalCuentas,
                PaginaActual = page,
                TotalPaginas = totalPaginas,
                PageSize = pageSize,
                Cuentas = saldosDto
            };
        }

    }
}
