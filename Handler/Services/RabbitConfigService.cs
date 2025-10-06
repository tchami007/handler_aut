namespace Handler.Services
{
    public interface IRabbitConfigService
    {
        RabbitConfig GetConfig();
        void UpdateConfig(object dto);
    }

    public class RabbitConfig
    {
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string Exchange { get; set; } = "handler_exchange";
    public List<ColaConfig> Colas { get; set; } = new();
    }

    public class ColaConfig
    {
        public string Nombre { get; set; } = string.Empty;
    }

    public class RabbitConfigService : IRabbitConfigService
    {
        private readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "RabbitConfig.json");
        private RabbitConfig? _config;

        public RabbitConfigService() {
            _config = LeerConfig();
        }

        public RabbitConfigService(RabbitConfig config) {
            _config = config;
        }

        public RabbitConfig GetConfig() => _config ?? new RabbitConfig();

        public void UpdateConfig(object dto)
        {
            if (dto is not null)
            {
                var props = dto.GetType().GetProperties();
                foreach (var prop in props)
                {
                    var value = prop.GetValue(dto);
                    var configProp = typeof(RabbitConfig).GetProperty(prop.Name);
                    if (configProp != null && value != null)
                    {
                        if (prop.Name == "Colas" && value is IEnumerable<object> colas)
                        {
                            var lista = new List<ColaConfig>();
                            var nombres = new HashSet<string>();
                            foreach (var c in colas)
                            {
                                var nombreProp = c.GetType().GetProperty("Nombre");
                                var nombre = nombreProp?.GetValue(c)?.ToString() ?? string.Empty;
                                if (string.IsNullOrWhiteSpace(nombre)) continue;
                                if (nombres.Contains(nombre))
                                    throw new Exception($"Nombre de cola duplicado: {nombre}");
                                nombres.Add(nombre);
                                lista.Add(new ColaConfig { Nombre = nombre });
                            }
                            if (lista.Count > 20)
                                throw new Exception("No se permiten más de 20 colas configuradas. Contacte al administrador para ampliar el límite.");
                            configProp.SetValue(_config, lista);
                        }
                        else
                        {
                            configProp.SetValue(_config, value);
                        }
                    }
                }
                GuardarConfig(_config!);
            }
        }

        private RabbitConfig LeerConfig()
        {
            if (!File.Exists(_configPath))
                return new RabbitConfig();
            var json = File.ReadAllText(_configPath);
            return System.Text.Json.JsonSerializer.Deserialize<RabbitConfig>(json) ?? new RabbitConfig();
        }

        private void GuardarConfig(RabbitConfig config)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }
    }
}
