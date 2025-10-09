using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Handler.Controllers.Dtos;

namespace Handler.Services
{
    public interface IConfigColasService
    {
        RabbitConfigFullDto GetConfig();
        void SetColas(List<ColaDto> nuevasColas);
        void EliminarUltimaCola();
        string AgregarCola();
    }

    public class ConfigColasService : IConfigColasService
    {
        private readonly string _configPath;

        public ConfigColasService(IConfiguration config)
        {
            _configPath = config["ConfigPath"] ?? throw new Exception("ConfigPath no definido en appsettings.json");
        }

        public RabbitConfigFullDto GetConfig()
        {
            if (!File.Exists(_configPath))
                throw new FileNotFoundException($"No se encontró el archivo de configuración en: {_configPath}");
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<RabbitConfigFullDto>(json) ?? new RabbitConfigFullDto();
        }

        public void SetColas(List<ColaDto> nuevasColas)
        {
            // Límite máximo de colas basado en archivos de configuración del Worker disponibles
            const int MAX_COLAS = 10;
            if (nuevasColas.Count > MAX_COLAS)
                throw new InvalidOperationException($"No se pueden configurar más de {MAX_COLAS} colas. Cantidad recibida: {nuevasColas.Count}");
            
            var config = GetConfig();
            config.Colas = nuevasColas;
            config.CantidadColas = nuevasColas.Count;
            GuardarConfig(config);
        }

        public void EliminarUltimaCola()
        {
            var config = GetConfig();
            if (config.Colas.Count == 0)
                throw new InvalidOperationException("No hay colas para eliminar.");
            config.Colas.RemoveAt(config.Colas.Count - 1);
            config.CantidadColas = config.Colas.Count;
            GuardarConfig(config);
        }

        public string AgregarCola()
        {
            var config = GetConfig();
            
            // Límite máximo de colas basado en archivos de configuración del Worker disponibles
            const int MAX_COLAS = 10;
            if (config.Colas.Count >= MAX_COLAS)
                throw new InvalidOperationException($"No se pueden agregar más colas. Límite máximo: {MAX_COLAS}");
            
            int siguiente = 1;
            if (config.Colas.Count > 0)
            {
                var max = config.Colas
                    .Select(c => {
                        var match = System.Text.RegularExpressions.Regex.Match(c.Nombre, @"cola_(\d+)");
                        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
                    })
                    .DefaultIfEmpty(0)
                    .Max();
                siguiente = max + 1;
            }
            var nuevoNombre = $"cola_{siguiente}";
            if (config.Colas.Any(c => c.Nombre == nuevoNombre))
                throw new InvalidOperationException($"La cola '{nuevoNombre}' ya existe.");
            config.Colas.Add(new Handler.Controllers.Dtos.ColaDto { Nombre = nuevoNombre });
            config.CantidadColas = config.Colas.Count;
            GuardarConfig(config);
            return nuevoNombre;
        }

        private void GuardarConfig(RabbitConfigFullDto config)
        {
            var nuevoJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, nuevoJson);
        }
    }

    // ...existing code...
}
