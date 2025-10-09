using Handler.Models;
using Microsoft.EntityFrameworkCore;

namespace Handler.Infrastructure
{
    /// <summary>
    /// Implementación del repositorio para entidades SolicitudDebito.
    /// Maneja todas las operaciones de acceso a datos relacionadas con solicitudes.
    /// </summary>
    public class SolicitudRepository : ISolicitudRepository
    {
        private readonly HandlerDbContext _context;

        public SolicitudRepository(HandlerDbContext context)
        {
            _context = context;
        }

        // Consultas básicas
        public async Task<SolicitudDebito?> GetByIdAsync(int id)
        {
            return await _context.SolicitudesDebito.FindAsync(id);
        }

        public async Task<List<SolicitudDebito>> GetAllAsync()
        {
            return await _context.SolicitudesDebito
                .OrderByDescending(s => s.FechaReal)
                .ToListAsync();
        }

        public async Task<List<SolicitudDebito>> GetByCuentaIdAsync(int cuentaId)
        {
            return await _context.SolicitudesDebito
                .Where(s => s.CuentaId == cuentaId)
                .OrderBy(s => s.FechaReal)
                .ToListAsync();
        }

        // Consultas por estado
        public async Task<List<SolicitudDebito>> GetByEstadoAsync(string estado)
        {
            return await _context.SolicitudesDebito
                .Where(s => s.Estado == estado)
                .OrderByDescending(s => s.FechaReal)
                .ToListAsync();
        }

        public async Task<List<SolicitudDebito>> GetPendientesAsync()
        {
            return await GetByEstadoAsync("pendiente");
        }

        public async Task<List<SolicitudDebito>> GetAutorizadasAsync()
        {
            return await GetByEstadoAsync("autorizada");
        }

        public async Task<List<SolicitudDebito>> GetRechazadasAsync()
        {
            return await GetByEstadoAsync("rechazada");
        }

        // Consultas por fecha
        public async Task<List<SolicitudDebito>> GetByFechaSolicitudAsync(DateTime fecha)
        {
            return await _context.SolicitudesDebito
                .Where(s => s.FechaSolicitud.Date == fecha.Date)
                .OrderByDescending(s => s.FechaReal)
                .ToListAsync();
        }

        public async Task<List<SolicitudDebito>> GetByRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            return await _context.SolicitudesDebito
                .Where(s => s.FechaSolicitud.Date >= fechaInicio.Date && 
                           s.FechaSolicitud.Date <= fechaFin.Date)
                .OrderByDescending(s => s.FechaReal)
                .ToListAsync();
        }

        // Consultas por cuenta específica
        public async Task<List<SolicitudDebito>> GetByCuentaNumeroAsync(long numeroCuenta)
        {
            return await _context.SolicitudesDebito
                .Include(s => s)
                .Where(s => _context.Cuentas.Any(c => c.Id == s.CuentaId && c.Numero == numeroCuenta))
                .OrderBy(s => s.FechaReal)
                .ToListAsync();
        }

        public async Task<List<SolicitudDebito>> GetProcessedByCuentaAsync(long numeroCuenta)
        {
            return await _context.SolicitudesDebito
                .Where(s => _context.Cuentas.Any(c => c.Id == s.CuentaId && c.Numero == numeroCuenta))
                .Where(s => s.Estado == "autorizada" || s.Estado == "rechazada")
                .OrderBy(s => s.FechaReal)
                .ToListAsync();
        }

        // Validaciones de negocio
        public async Task<bool> ExisteSolicitudAutorizadaAsync(int cuentaId, decimal monto, long numeroComprobante, DateTime fecha)
        {
            return await _context.SolicitudesDebito
                .AnyAsync(s => s.CuentaId == cuentaId &&
                              s.Monto == monto &&
                              s.NumeroComprobante == numeroComprobante &&
                              s.FechaSolicitud.Date == fecha.Date &&
                              s.Estado == "autorizada");
        }

        public async Task<SolicitudDebito?> GetUltimaSolicitudByCuentaAsync(int cuentaId)
        {
            return await _context.SolicitudesDebito
                .Where(s => s.CuentaId == cuentaId)
                .OrderByDescending(s => s.FechaReal)
                .FirstOrDefaultAsync();
        }

        // Consultas para estadísticas
        public async Task<int> GetCountByEstadoAsync(string estado)
        {
            return await _context.SolicitudesDebito
                .CountAsync(s => s.Estado == estado);
        }

        public async Task<int> GetCountByTipoMovimientoAsync(string tipoMovimiento)
        {
            return await _context.SolicitudesDebito
                .CountAsync(s => s.TipoMovimiento == tipoMovimiento);
        }

        public async Task<decimal> GetSumaMontosByEstadoAsync(string estado)
        {
            return await _context.SolicitudesDebito
                .Where(s => s.Estado == estado)
                .SumAsync(s => s.Monto);
        }

        public async Task<List<SolicitudDebito>> GetSolicitudesProcesamientoAsync()
        {
            return await _context.SolicitudesDebito
                .Where(s => s.Estado == "autorizada" || s.Estado == "rechazada")
                .OrderByDescending(s => s.FechaReal)
                .ToListAsync();
        }

        // Operaciones de modificación
        public async Task AddAsync(SolicitudDebito solicitud)
        {
            await _context.SolicitudesDebito.AddAsync(solicitud);
        }

        public async Task UpdateAsync(SolicitudDebito solicitud)
        {
            _context.SolicitudesDebito.Update(solicitud);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            var solicitud = await GetByIdAsync(id);
            if (solicitud != null)
            {
                _context.SolicitudesDebito.Remove(solicitud);
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        // Para operaciones de inicialización
        public async Task DeleteAllAsync()
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM SolicitudesDebito");
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.SolicitudesDebito.CountAsync();
        }
    }
}