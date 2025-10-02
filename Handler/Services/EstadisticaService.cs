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
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Handler", "Config", "RabbitConfig.json");
            if (!File.Exists(configPath))
                return new List<string>();
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
            var solicitudesQuery = _db.SolicitudesDebito.AsQueryable();
            if (!string.IsNullOrEmpty(tipoMovimiento))
            {
                solicitudesQuery = solicitudesQuery.Where(s => s.TipoMovimiento == tipoMovimiento);
            }
            var solicitudesProcesadas = await solicitudesQuery.CountAsync();
            var solicitudesPorEstado = await solicitudesQuery
                .GroupBy(s => s.Estado)
                .Select(g => new { Estado = g.Key ?? "", Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.Estado, x => x.Cantidad);
            var saldoTotalCuentas = await _db.Cuentas.SumAsync(c => c.Saldo);
            var movimientosPorTipo = await _db.SolicitudesDebito
                .GroupBy(m => m.TipoMovimiento ?? "")
                .Select(g => new { Tipo = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.Tipo, x => x.Cantidad);
            var errores = await _db.LogsOperacion.CountAsync(l => l.Tipo == "error");
            var logsRecientes = await _db.LogsOperacion
                .OrderByDescending(l => l.Fecha)
                .Take(10)
                .Select(l => new LogDto { Fecha = l.Fecha, Nivel = l.Tipo ?? "", Mensaje = l.Mensaje ?? "" })
                .ToListAsync();

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
