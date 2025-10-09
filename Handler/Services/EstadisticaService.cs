using Handler.Infrastructure;
using Handler.Models;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Handler.Services
{
    public interface IEstadisticaService
    {
    Task<EstadisticaDto> GetEstadisticasAsync(string? tipoMovimiento = null);
    }

    public class EstadisticaService : IEstadisticaService
    {
        private readonly ISolicitudRepository _solicitudRepository;
        private readonly ICuentaRepository _cuentaRepository;
        private readonly ILogOperacionRepository _logRepository;
        private readonly IConnection _rabbitConnection;
        private readonly ILogger<EstadisticaService> _logger;

        public EstadisticaService(ISolicitudRepository solicitudRepository, ICuentaRepository cuentaRepository, ILogOperacionRepository logRepository, IConnection rabbitConnection, ILogger<EstadisticaService> logger)
        {
            _solicitudRepository = solicitudRepository;
            _cuentaRepository = cuentaRepository;
            _logRepository = logRepository;
            _rabbitConnection = rabbitConnection;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene la lista de nombres de colas desde Handler/Config/RabbitConfig.json
        /// </summary>
        private List<string> ObtenerNombresColasDesdeConfig()
        {
            // Buscar la carpeta handler_aut en la ruta actual
            var projectRoot = AppDomain.CurrentDomain.BaseDirectory;
            var dir = new DirectoryInfo(projectRoot);
            while (dir != null && !string.Equals(dir.Name, "handler_aut", StringComparison.OrdinalIgnoreCase))
                dir = dir.Parent;
            string configPath;
            if (dir != null)
                configPath = Path.Combine(dir.FullName, "Handler", "Config", "RabbitConfig.json");
            else
                configPath = Path.Combine(projectRoot, "Handler", "Config", "RabbitConfig.json");
            if (!File.Exists(configPath))
            {
                _logger.LogWarning("No se encontró el archivo de configuración de RabbitMQ en {ConfigPath}", configPath);
                return new List<string>();
            }
            var json = File.ReadAllText(configPath);
            var config = System.Text.Json.JsonSerializer.Deserialize<RabbitConfigJson>(json);
            return config?.Colas?.Select(c => c.Nombre).ToList() ?? new List<string>();
        }

        private string ObtenerRutaConfigOriginal()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Handler", "Config", "RabbitConfig.json");
        }

        private class RabbitConfigJson
        {
            public List<ColaJson> Colas { get; set; } = new();
        }
        private class ColaJson
        {
            public string Nombre { get; set; } = string.Empty;
        }

        public async Task<EstadisticaDto> GetEstadisticasAsync(string? tipoMovimiento = null)
        {
            // Obtener todas las solicitudes
            var todasSolicitudes = await _solicitudRepository.GetAllAsync();
            
            // Filtrar por tipo de movimiento si se proporciona
            if (!string.IsNullOrEmpty(tipoMovimiento))
            {
                todasSolicitudes = todasSolicitudes
                    .Where(s => s.TipoMovimiento == tipoMovimiento)
                    .ToList();
            }

            // Obtener cantidad de solicitudes procesadas
            var solicitudesProcesadas = todasSolicitudes.Count;

            // Obtener cantidad de solicitudes por estado
            var solicitudesPorEstado = todasSolicitudes
                .GroupBy(s => s.Estado ?? "")
                .ToDictionary(g => g.Key, g => g.Count());

            // Obtener saldo total de todas las cuentas
            var todasCuentas = await _cuentaRepository.GetAllAsync();
            var saldoTotalCuentas = todasCuentas.Sum(c => c.Saldo);

            // Obtener cantidad de movimientos por tipo
            var todasSolicitudesParaTipos = await _solicitudRepository.GetAllAsync();
            var movimientosPorTipo = todasSolicitudesParaTipos
                .GroupBy(m => m.TipoMovimiento ?? "")
                .ToDictionary(g => g.Key, g => g.Count());

            // Obtener cantidad de errores registrados
            var errores = await _logRepository.GetCountByTipoAsync("error");

            // Obtener los 10 logs más recientes
            var logsRecientesRaw = await _logRepository.GetRecientesAsync(10);
            var logsRecientes = logsRecientesRaw
                .Select(l => new LogDto { 
                    Fecha = l.Fecha, 
                    Nivel = l.Tipo ?? "", 
                    Mensaje = l.Mensaje ?? "" 
                })
                .ToList();

            // Obtener estado de las colas en RabbitMQ      
            var colasRabbit = new List<ColaRabbitDto>();
            var nombresColas = ObtenerNombresColasDesdeConfig();
            using (var channel = _rabbitConnection.CreateModel())
            {
                foreach (var cola in nombresColas)
                {
                    try
                    {
                        var queue = channel.QueueDeclarePassive(cola);
                        colasRabbit.Add(new ColaRabbitDto
                        {
                            Nombre = cola,
                            MensajesPendientes = (int)queue.MessageCount,
                            Consumidores = (int)queue.ConsumerCount
                        });
                    }
                    catch
                    {
                        colasRabbit.Add(new ColaRabbitDto
                        {
                            Nombre = cola,
                            MensajesPendientes = -1,
                            Consumidores = -1
                        });
                    }
                }
            }
            // Construir y retornar el DTO de estadísticas
            return new EstadisticaDto
            {
                SolicitudesProcesadas = solicitudesProcesadas,
                SolicitudesPorEstado = solicitudesPorEstado,
                SaldoTotalCuentas = saldoTotalCuentas,
                MovimientosPorTipo = movimientosPorTipo,
                Errores = errores,
                ColasRabbit = colasRabbit,
                LogsRecientes = logsRecientes
            };
        }
    }

    public class EstadisticaDto
    {
        public int SolicitudesProcesadas { get; set; }
        public Dictionary<string, int> SolicitudesPorEstado { get; set; } = new();
        public decimal SaldoTotalCuentas { get; set; }
        public Dictionary<string, int> MovimientosPorTipo { get; set; } = new();
    //public int TiempoPromedioProcesamientoMs { get; set; } // No implementado
    public int Errores { get; set; }
    //public Dictionary<string, int> RechazosPorMotivo { get; set; } = new(); // No implementado
        public List<ColaRabbitDto> ColasRabbit { get; set; } = new();
        public List<LogDto> LogsRecientes { get; set; } = new();
    }

    public class ColaRabbitDto
    {
        public string Nombre { get; set; } = string.Empty;
        public int MensajesPendientes { get; set; }
        public int Consumidores { get; set; }
    }

    public class LogDto
    {
        public DateTime Fecha { get; set; }
        public string Nivel { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }
}
