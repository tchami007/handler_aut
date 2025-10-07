namespace Handler.Controllers.Dtos
{
    public class SolicitudResultadoDto
    {
        public int Id { get; set; }
        public decimal Saldo { get; set; }
        public int Status { get; set; }
        public string Cola { get; set; } = string.Empty;
    }
}