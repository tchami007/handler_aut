using Handler.Controllers.Dtos;
using System.Collections.Generic;

namespace Handler.Services
{
    public interface ISaldoService
    {
    SaldoCuentaDto? GetSaldoByCuenta(long numeroCuenta);
    List<SaldoCuentaDto> GetSaldoAll();
    SaldoCuentaPaginadoDto GetSaldoPaginado(int page, int pageSize);
    }
}
