using Handler.Models;
using Microsoft.EntityFrameworkCore;

namespace Handler.Infrastructure
{
    /// <summary>
    /// Implementación del repositorio para entidades LogOperacion.
    /// Maneja todas las operaciones de acceso a datos relacionadas con logging.
    /// </summary>
    public class LogOperacionRepository : ILogOperacionRepository
    {
        private readonly HandlerDbContext _context;

        public LogOperacionRepository(HandlerDbContext context)
        {
            _context = context;
        }

        // Consultas básicas
        public async Task<LogOperacion?> GetByIdAsync(int id)
        {
            return await _context.LogsOperacion.FindAsync(id);
        }

        public async Task<List<LogOperacion>> GetAllAsync()
        {
            return await _context.LogsOperacion
                .OrderByDescending(l => l.Fecha)
                .ToListAsync();
        }

        // Consultas por tipo
        public async Task<List<LogOperacion>> GetByTipoAsync(string tipo)
        {
            return await _context.LogsOperacion
                .Where(l => l.Tipo == tipo)
                .OrderByDescending(l => l.Fecha)
                .ToListAsync();
        }

        public async Task<List<LogOperacion>> GetInfoLogsAsync()
        {
            return await GetByTipoAsync("info");
        }

        public async Task<List<LogOperacion>> GetErrorLogsAsync()
        {
            return await GetByTipoAsync("error");
        }

        public async Task<List<LogOperacion>> GetAuditoriaLogsAsync()
        {
            return await GetByTipoAsync("auditoria");
        }

        // Consultas por fecha
        public async Task<List<LogOperacion>> GetByFechaAsync(DateTime fecha)
        {
            return await _context.LogsOperacion
                .Where(l => l.Fecha.Date == fecha.Date)
                .OrderByDescending(l => l.Fecha)
                .ToListAsync();
        }

        public async Task<List<LogOperacion>> GetByRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            return await _context.LogsOperacion
                .Where(l => l.Fecha.Date >= fechaInicio.Date && 
                           l.Fecha.Date <= fechaFin.Date)
                .OrderByDescending(l => l.Fecha)
                .ToListAsync();
        }

        public async Task<List<LogOperacion>> GetRecientesAsync(int cantidad = 100)
        {
            return await _context.LogsOperacion
                .OrderByDescending(l => l.Fecha)
                .Take(cantidad)
                .ToListAsync();
        }

        // Consultas con paginación
        public async Task<List<LogOperacion>> GetPaginatedAsync(int skip, int take)
        {
            return await _context.LogsOperacion
                .OrderByDescending(l => l.Fecha)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.LogsOperacion.CountAsync();
        }

        // Búsquedas de texto
        public async Task<List<LogOperacion>> GetByMensajeContainsAsync(string texto)
        {
            return await _context.LogsOperacion
                .Where(l => l.Mensaje != null && l.Mensaje.Contains(texto))
                .OrderByDescending(l => l.Fecha)
                .ToListAsync();
        }

        // Estadísticas
        public async Task<int> GetCountByTipoAsync(string tipo)
        {
            return await _context.LogsOperacion
                .CountAsync(l => l.Tipo == tipo);
        }

        public async Task<Dictionary<string, int>> GetCountByTipoGroupedAsync()
        {
            return await _context.LogsOperacion
                .Where(l => l.Tipo != null)
                .GroupBy(l => l.Tipo)
                .Select(g => new { Tipo = g.Key!, Count = g.Count() })
                .ToDictionaryAsync(x => x.Tipo, x => x.Count);
        }

        // Operaciones de modificación
        public async Task AddAsync(LogOperacion log)
        {
            await _context.LogsOperacion.AddAsync(log);
        }

        public async Task AddRangeAsync(IEnumerable<LogOperacion> logs)
        {
            await _context.LogsOperacion.AddRangeAsync(logs);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        // Limpieza y mantenimiento
        public async Task DeleteOlderThanAsync(DateTime fecha)
        {
            var logsToDelete = await _context.LogsOperacion
                .Where(l => l.Fecha < fecha)
                .ToListAsync();
            
            _context.LogsOperacion.RemoveRange(logsToDelete);
        }

        public async Task DeleteByTipoAsync(string tipo)
        {
            var logsToDelete = await _context.LogsOperacion
                .Where(l => l.Tipo == tipo)
                .ToListAsync();
            
            _context.LogsOperacion.RemoveRange(logsToDelete);
        }

        public async Task DeleteAllAsync()
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM LogsOperacion");
        }
    }
}