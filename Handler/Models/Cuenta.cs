namespace Handler.Models
{
    public class Cuenta
    {
        public int Id { get; set; }
        public long Numero { get; set; } // Usar long para compatibilidad con numeric(17,0)
        public decimal Saldo { get; set; }

        /// <summary>
        /// Token de concurrencia optimista. Se actualiza automáticamente en cada modificación.
        /// </summary>
        public byte[]? RowVersion { get; set; }
    }
}
