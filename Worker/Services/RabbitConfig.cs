namespace Worker.Services
{
    public class RabbitConfig
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
        public string Exchange { get; set; } = "handler_exchange";
        public int CantidadColas { get; set; } = 1;
        public List<ColaConfig> Colas { get; set; } = new();
    }

    public class ColaConfig
    {
        public string Nombre { get; set; } = string.Empty;
    }
}
