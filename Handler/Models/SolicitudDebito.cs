namespace Handler.Models
{
    public class SolicitudDebito
    {
    public int Id { get; set; }
    public int CuentaId { get; set; }
    public decimal Monto { get; set; }
    public DateTime FechaSolicitud { get; set; }
    internal DateTime FechaReal { get; set; }
    public string? Estado { get; set; } // pendiente, autorizada, rechazada
    public int CodigoEstado { get; set; } // Nuevo: código de estado asignado por el servicio
    public string TipoMovimiento { get; set; } = "debito"; // debito, credito, contrasiento_debito, contrasiento_credito
    public int? MovimientoOriginalId { get; set; } // Para contrasientos, referencia al movimiento original
    public long NumeroComprobante { get; set; }
    /// <summary>
    /// Saldo de la cuenta luego de aplicar la solicitud
    /// </summary>
    public decimal SaldoRespuesta { get; set; }
    }
}
