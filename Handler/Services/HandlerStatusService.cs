namespace Handler.Services
{
    public interface IHandlerStatusService
    {
        object GetHealthInfo();
        void Activar();
        void Inactivar();
        bool EstaActivo();
    }

    public class HandlerStatusService : IHandlerStatusService
    {
        private static bool _activo = true;

        public object GetHealthInfo()
        {
            return new
            {
                estado = _activo ? "activo" : "inactivo",
                version = typeof(HandlerStatusService).Assembly.GetName().Version?.ToString() ?? "1.0.0",
                fecha = DateTime.Now
            };
        }

        public void Activar()
        {
            _activo = true;
        }

        public void Inactivar()
        {
            _activo = false;
        }

        public bool EstaActivo() => _activo;
    }
}
