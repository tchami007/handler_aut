using Microsoft.EntityFrameworkCore;
using Handler.Models;

namespace Handler.Infrastructure
{
    public class HandlerDbContext : DbContext
    {
        public HandlerDbContext(DbContextOptions<HandlerDbContext> options) : base(options) { }

        public DbSet<Cuenta> Cuentas { get; set; }
        public DbSet<SolicitudDebito> SolicitudesDebito { get; set; }
        public DbSet<LogOperacion> LogsOperacion { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cuenta>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Numero)
                      .IsRequired();
                entity.Property(e => e.Saldo)
                      .IsRequired();
            });

            modelBuilder.Entity<SolicitudDebito>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Monto)
                      .IsRequired();
                entity.Property(e => e.Estado)
                      .HasMaxLength(20)
                      .IsRequired();
                entity.Property(e => e.FechaSolicitud)
                      .IsRequired();
                entity.HasOne<Cuenta>()
                      .WithMany()
                      .HasForeignKey(e => e.CuentaId);
            });

            modelBuilder.Entity<LogOperacion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Fecha)
                      .IsRequired();
                entity.Property(e => e.Mensaje)
                      .HasMaxLength(255)
                      .IsRequired();
                entity.Property(e => e.Tipo)
                      .HasMaxLength(20)
                      .IsRequired();
            });
        }
    }
}
