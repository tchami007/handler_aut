using Handler.Infrastructure;
using Handler.Models;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Handler.Services
{
    public interface IEstadisticaService
    {
    Task<EstadisticaDto> GetEstadisticasAsync(string? tipoMovimiento = null);
    }

    public class EstadisticaService : IEstadisticaService
    {
        private readonly HandlerDbContext _db;
        private readonly IConnection _rabbitConnection;

        public EstadisticaService(HandlerDbContext db, IConnection rabbitConnection)
        {
            _db = db;
            _rabbitConnection = rabbitConnection;
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
                Console.WriteLine($"Advertencia: No se encontró el archivo de configuración de RabbitMQ en {configPath}");
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
            // Filtrar solicitudes por tipo de movimiento si se proporciona
            var solicitudesQuery = _db.SolicitudesDebito.AsQueryable();

            // Aplicar filtro por tipo de movimiento
            if (!string.IsNullOrEmpty(tipoMovimiento))
            {
                // Aplicar filtro por tipo de movimiento
                solicitudesQuery = solicitudesQuery.Where(s => s.TipoMovimiento == tipoMovimiento);
            }

            // Obtener cantidad de solicitudes procesadas
            var solicitudesProcesadas = await solicitudesQuery.CountAsync();

            // Obtener cantidad de solicitudes por estado
            var solicitudesPorEstado = await solicitudesQuery
                .GroupBy(s => s.Estado)
                .Select(g => new { Estado = g.Key ?? "", Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.Estado, x => x.Cantidad);

            // Obtener saldo total de todas las cuentas
            var saldoTotalCuentas = await _db.Cuentas.SumAsync(c => c.Saldo);

            // Obtener cantidad de movimientos por tipo
            var movimientosPorTipo = await _db.SolicitudesDebito
                .GroupBy(m => m.TipoMovimiento ?? "")
                .Select(g => new { Tipo = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.Tipo, x => x.Cantidad);

            // Obtener cantidad de errores registrados
            var errores = await _db.LogsOperacion.CountAsync(l => l.Tipo == "error");

            // Obtener los 10 logs más recientes
            var logsRecientes = await _db.LogsOperacion
                .OrderByDescending(l => l.Fecha)
                .Take(10)
                .Select(l => new LogDto { Fecha = l.Fecha, Nivel = l.Tipo ?? "", Mensaje = l.Mensaje ?? "" })
                .ToListAsync();

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
