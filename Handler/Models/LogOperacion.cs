namespace Handler.Models
{
    public class LogOperacion
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string? Mensaje { get; set; }
        public string? Tipo { get; set; } // info, error, auditoria
    }
}
