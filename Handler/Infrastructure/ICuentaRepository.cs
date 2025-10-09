using Handler.Models;

namespace Handler.Infrastructure
{
    /// <summary>
    /// Repositorio para el manejo de entidades Cuenta.
    /// Proporciona métodos de acceso a datos específicos del dominio de cuentas.
    /// </summary>
    public interface ICuentaRepository
    {
        // Consultas básicas
        Task<Cuenta?> GetByIdAsync(int id);
        Task<Cuenta?> GetByNumeroAsync(long numero);
        Task<List<Cuenta>> GetAllAsync();
        Task<bool> ExistsAsync(long numero);
        
        // Consultas con paginación
        Task<List<Cuenta>> GetPaginatedAsync(int skip, int take);
        Task<int> GetTotalCountAsync();
        
        // Operaciones de modificación
        Task AddAsync(Cuenta cuenta);
        Task UpdateAsync(Cuenta cuenta);
        Task DeleteAsync(int id);
        Task<int> SaveChangesAsync();
        
        // Consultas específicas del dominio
        Task<List<Cuenta>> GetByNumeroRangeAsync(long numeroInicio, long numeroFin);
        Task<List<Cuenta>> GetBySaldoMinimoAsync(decimal saldoMinimo);
        Task<decimal> GetSaldoTotalAsync();
        
        // Para operaciones de inicialización
        Task DeleteAllAsync();
        Task AddRangeAsync(IEnumerable<Cuenta> cuentas);
    }
}