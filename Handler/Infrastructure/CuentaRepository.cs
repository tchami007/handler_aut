using Handler.Models;
using Microsoft.EntityFrameworkCore;

namespace Handler.Infrastructure
{
    /// <summary>
    /// Implementación del repositorio para entidades Cuenta.
    /// Maneja todas las operaciones de acceso a datos relacionadas con cuentas.
    /// </summary>
    public class CuentaRepository : ICuentaRepository
    {
        private readonly HandlerDbContext _context;

        public CuentaRepository(HandlerDbContext context)
        {
            _context = context;
        }

        // Consultas básicas
        public async Task<Cuenta?> GetByIdAsync(int id)
        {
            return await _context.Cuentas.FindAsync(id);
        }

        public async Task<Cuenta?> GetByNumeroAsync(long numero)
        {
            return await _context.Cuentas
                .FirstOrDefaultAsync(c => c.Numero == numero);
        }

        public async Task<List<Cuenta>> GetAllAsync()
        {
            return await _context.Cuentas
                .OrderBy(c => c.Numero)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(long numero)
        {
            return await _context.Cuentas
                .AnyAsync(c => c.Numero == numero);
        }

        // Consultas con paginación
        public async Task<List<Cuenta>> GetPaginatedAsync(int skip, int take)
        {
            return await _context.Cuentas
                .OrderBy(c => c.Numero)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.Cuentas.CountAsync();
        }

        // Operaciones de modificación
        public async Task AddAsync(Cuenta cuenta)
        {
            await _context.Cuentas.AddAsync(cuenta);
        }

        public async Task UpdateAsync(Cuenta cuenta)
        {
            _context.Cuentas.Update(cuenta);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            var cuenta = await GetByIdAsync(id);
            if (cuenta != null)
            {
                _context.Cuentas.Remove(cuenta);
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        // Consultas específicas del dominio
        public async Task<List<Cuenta>> GetByNumeroRangeAsync(long numeroInicio, long numeroFin)
        {
            return await _context.Cuentas
                .Where(c => c.Numero >= numeroInicio && c.Numero <= numeroFin)
                .OrderBy(c => c.Numero)
                .ToListAsync();
        }

        public async Task<List<Cuenta>> GetBySaldoMinimoAsync(decimal saldoMinimo)
        {
            return await _context.Cuentas
                .Where(c => c.Saldo >= saldoMinimo)
                .OrderBy(c => c.Numero)
                .ToListAsync();
        }

        public async Task<decimal> GetSaldoTotalAsync()
        {
            return await _context.Cuentas
                .SumAsync(c => c.Saldo);
        }

        // Para operaciones de inicialización
        public async Task DeleteAllAsync()
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM Cuentas");
        }

        public async Task AddRangeAsync(IEnumerable<Cuenta> cuentas)
        {
            await _context.Cuentas.AddRangeAsync(cuentas);
        }
    }
}