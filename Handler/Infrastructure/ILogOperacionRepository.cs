using Handler.Models;

namespace Handler.Infrastructure
{
    /// <summary>
    /// Repositorio para el manejo de entidades LogOperacion.
    /// Proporciona métodos de acceso a datos específicos del dominio de logging.
    /// </summary>
    public interface ILogOperacionRepository
    {
        // Consultas básicas
        Task<LogOperacion?> GetByIdAsync(int id);
        Task<List<LogOperacion>> GetAllAsync();
        
        // Consultas por tipo
        Task<List<LogOperacion>> GetByTipoAsync(string tipo);
        Task<List<LogOperacion>> GetInfoLogsAsync();
        Task<List<LogOperacion>> GetErrorLogsAsync();
        Task<List<LogOperacion>> GetAuditoriaLogsAsync();
        
        // Consultas por fecha
        Task<List<LogOperacion>> GetByFechaAsync(DateTime fecha);
        Task<List<LogOperacion>> GetByRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin);
        Task<List<LogOperacion>> GetRecientesAsync(int cantidad = 100);
        
        // Consultas con paginación
        Task<List<LogOperacion>> GetPaginatedAsync(int skip, int take);
        Task<int> GetTotalCountAsync();
        
        // Búsquedas de texto
        Task<List<LogOperacion>> GetByMensajeContainsAsync(string texto);
        
        // Estadísticas
        Task<int> GetCountByTipoAsync(string tipo);
        Task<Dictionary<string, int>> GetCountByTipoGroupedAsync();
        
        // Operaciones de modificación
        Task AddAsync(LogOperacion log);
        Task AddRangeAsync(IEnumerable<LogOperacion> logs);
        Task<int> SaveChangesAsync();
        
        // Limpieza y mantenimiento
        Task DeleteOlderThanAsync(DateTime fecha);
        Task DeleteByTipoAsync(string tipo);
        Task DeleteAllAsync();
    }
}