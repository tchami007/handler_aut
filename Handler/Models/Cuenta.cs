namespace Handler.Models
{
    public class Cuenta
    {
        public int Id { get; set; }
        public long Numero { get; set; } // Usar long para compatibilidad con numeric(17,0)
        public decimal Saldo { get; set; }
    }
}
