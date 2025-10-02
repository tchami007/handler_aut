namespace Handler.Controllers.Dtos
{
    public class SaldoCuentaPaginadoDto
    {
        public int TotalCuentas { get; set; }
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public int PageSize { get; set; }
        public List<SaldoCuentaDto> Cuentas { get; set; } = new();
    }
}
