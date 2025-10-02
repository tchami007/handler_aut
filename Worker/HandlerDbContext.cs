using Microsoft.EntityFrameworkCore;

namespace Worker
{

    public class HandlerDbContext : DbContext
    {
        public HandlerDbContext(DbContextOptions<HandlerDbContext> options) : base(options) { }

        public DbSet<Cuenta> Cuentas { get; set; }
        public DbSet<SolicitudDebito> SolicitudesDebito { get; set; }
        public DbSet<Movimiento> Movimientos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cuenta>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Numero)
                    .IsRequired();
                entity.Property(e => e.Saldo)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<SolicitudDebito>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Monto)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");
                entity.Property(e => e.Estado)
                    .HasMaxLength(20)
                    .IsRequired(false);
                entity.Property(e => e.FechaSolicitud)
                    .IsRequired();
                entity.Property(e => e.FechaReal)
                    .IsRequired();
                entity.Property(e => e.NumeroComprobante)
                    .IsRequired()
                    .HasColumnType("numeric(10,0)");
                entity.Property(e => e.SaldoRespuesta)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");
                entity.Property(e => e.CodigoEstado)
                    .IsRequired();
                entity.HasOne<Cuenta>()
                    .WithMany()
                    .HasForeignKey(e => e.CuentaId);
            });
        }
    }

    public class Cuenta
    {
        public int Id { get; set; }
        public long Numero { get; set; } // Usar long para compatibilidad con numeric(17,0)
        public decimal Saldo { get; set; }
    }

    public class SolicitudDebito
    {
        public int Id { get; set; }
        public int CuentaId { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaSolicitud { get; set; }
        internal DateTime FechaReal { get; set; }
        public string? Estado { get; set; } // pendiente, autorizada, rechazada
        public int CodigoEstado { get; set; } // código de estado asignado por el servicio
        public string TipoMovimiento { get; set; } = "debito"; // debito, credito, contrasiento_debito, contrasiento_credito
        public int? MovimientoOriginalId { get; set; } // Para contrasientos, referencia al movimiento original
        public long NumeroComprobante { get; set; }
        /// <summary>
        /// Saldo de la cuenta luego de aplicar la solicitud
        /// </summary>
        public decimal SaldoRespuesta { get; set; }
    }
    
        public class Movimiento
    {
        public int Id { get; set; }
        public long NumeroCuenta { get; set; }
        public decimal Importe { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public string NumeroComprobante { get; set; } = string.Empty;
        public string? CodigoContrasiento { get; set; }
        public DateTime FechaReal { get; set; }
        public DateTime? FecEnlace { get; set; }
        public int FunCod { get; set; } // -1 para débito, +1 para crédito
    }
}
