using Handler.Models;

namespace Handler.Infrastructure
{
    /// <summary>
    /// Repositorio para el manejo de entidades SolicitudDebito.
    /// Proporciona métodos de acceso a datos específicos del dominio de solicitudes.
    /// </summary>
    public interface ISolicitudRepository
    {
        // Consultas básicas
        Task<SolicitudDebito?> GetByIdAsync(int id);
        Task<List<SolicitudDebito>> GetAllAsync();
        Task<List<SolicitudDebito>> GetByCuentaIdAsync(int cuentaId);
        
        // Consultas por estado
        Task<List<SolicitudDebito>> GetByEstadoAsync(string estado);
        Task<List<SolicitudDebito>> GetPendientesAsync();
        Task<List<SolicitudDebito>> GetAutorizadasAsync();
        Task<List<SolicitudDebito>> GetRechazadasAsync();
        
        // Consultas por fecha
        Task<List<SolicitudDebito>> GetByFechaSolicitudAsync(DateTime fecha);
        Task<List<SolicitudDebito>> GetByRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin);
        
        // Consultas por cuenta específica
        Task<List<SolicitudDebito>> GetByCuentaNumeroAsync(long numeroCuenta);
        Task<List<SolicitudDebito>> GetProcessedByCuentaAsync(long numeroCuenta);
        
        // Validaciones de negocio
        Task<bool> ExisteSolicitudAutorizadaAsync(int cuentaId, decimal monto, long numeroComprobante, DateTime fecha);
        Task<SolicitudDebito?> GetUltimaSolicitudByCuentaAsync(int cuentaId);
        
        // Consultas para estadísticas
        Task<int> GetCountByEstadoAsync(string estado);
        Task<int> GetCountByTipoMovimientoAsync(string tipoMovimiento);
        Task<decimal> GetSumaMontosByEstadoAsync(string estado);
        Task<List<SolicitudDebito>> GetSolicitudesProcesamientoAsync(); // Ordenadas por fecha real desc
        
        // Operaciones de modificación
        Task AddAsync(SolicitudDebito solicitud);
        Task UpdateAsync(SolicitudDebito solicitud);
        Task DeleteAsync(int id);
        Task<int> SaveChangesAsync();
        
        // Para operaciones de inicialización
        Task DeleteAllAsync();
        Task<int> GetTotalCountAsync();
    }
}