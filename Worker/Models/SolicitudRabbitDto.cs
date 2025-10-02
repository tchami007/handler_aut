namespace Worker.Models
{
    public class SolicitudRabbitDto
    {
        public int Id { get; set; }
        public long NumeroCuenta { get; set; }
        public decimal Importe { get; set; }
        public string TipoMovimiento { get; set; } = "debito";
        public long NumeroComprobante { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public string? Contrasiento { get; set; }
        public string? ConnectionStringBanksys { get; set; }
    }

}
