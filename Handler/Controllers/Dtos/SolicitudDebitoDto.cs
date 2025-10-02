using System;

namespace Handler.Controllers.Dtos
{
    public class SolicitudDebitoDto
    {
        public int Id { get; set; }
        public int CuentaId { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public DateTime FechaReal { get; set; }
        public string? Estado { get; set; }
        public int CodigoEstado { get; set; }
        public string TipoMovimiento { get; set; } = "debito";
        public int? MovimientoOriginalId { get; set; }
        public long NumeroComprobante { get; set; }
        public decimal SaldoRespuesta { get; set; }
    }
}