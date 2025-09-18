namespace Handler.Models
{
    public class SolicitudDebito
    {
        public int Id { get; set; }
        public int CuentaId { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public string? Estado { get; set; } // pendiente, autorizada, rechazada
    }
}
