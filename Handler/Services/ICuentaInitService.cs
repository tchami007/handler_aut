using System.Threading.Tasks;

namespace Handler.Services
{
    public interface ICuentaInitService
    {
        Task<int> InicializarCuentasAsync(int cantidad = 1000);
    }
}
