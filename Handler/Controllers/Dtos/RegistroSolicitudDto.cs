namespace Handler.Controllers.Dtos
{
    public class RegistroSolicitudDto
    {
        public long NumeroCuenta { get; set; }
        public decimal Monto { get; set; }
        public string TipoMovimiento { get; set; } = "debito"; // debito, credito, contrasiento_debito, contrasiento_credito
        public int? MovimientoOriginalId { get; set; }
    public long NumeroComprobante { get; set; }
    }
}
