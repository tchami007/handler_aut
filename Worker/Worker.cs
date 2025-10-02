using Worker.Services;
using Worker.Models;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RabbitMqConsumer _consumer;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        var cola = ObtenerNombreCola();
        _consumer = new RabbitMqConsumer(cola)!;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.StartConsuming(msg =>
        {
            _logger.LogInformation($"Mensaje recibido de RabbitMQ: {msg}");
            try
            {
                // Parsear el mensaje JSON a SolicitudRabbitDto
                var solicitud = ParseSolicitudRabbitDto(msg);
                if (solicitud == null)
                {
                    _logger.LogWarning("Solicitud inválida.");
                    return;
                }
                // Obtener la cadena de conexión adecuada
                string connectionString = ObtenerConnectionStringSegunSolicitud(solicitud);

                // Ejecutar el movimiento en la base de datos
                var resultado = EjecutarMovimiento(solicitud, connectionString);
                if (resultado.Exito && resultado.SaldoFinal.HasValue)
                {
                    _logger.LogInformation($"Procedimiento ejecutado correctamente para cuenta {solicitud.NumeroCuenta}, movimiento {solicitud.TipoMovimiento}. Saldo final: {resultado.SaldoFinal}");

                    // Actualizar saldo en Handler
                    ActualizarSaldoHandler(solicitud.NumeroCuenta, resultado.SaldoFinal.Value, ServiceProviderAccessor.Instance);
                    // Actualizar solicitud en Handler usando el Id
                    ActualizarSolicitudHandler(solicitud.Id, resultado.SaldoFinal.Value, ServiceProviderAccessor.Instance);
                }
                else if (!resultado.Exito)
                {
                    _logger.LogError(resultado.MensajeError);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error procesando solicitud: {ex.Message}\n{ex}");
            }
        });
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }


    private SolicitudRabbitDto? ParseSolicitudRabbitDto(string msg)
    {
        try
        {
            string jsonString = msg;
            int jsonStart = msg.IndexOf('{');
            if (jsonStart > 0)
                jsonString = msg.Substring(jsonStart);
            var jsonObj = System.Text.Json.JsonDocument.Parse(jsonString).RootElement;
            var solicitud = new SolicitudRabbitDto
            {
                Id = jsonObj.TryGetProperty("Id", out var idProp) ? idProp.GetInt32() : 0,
                NumeroCuenta = jsonObj.GetProperty("NumeroCuenta").GetInt64(),
                Importe = jsonObj.GetProperty("Importe").GetDecimal(),
                TipoMovimiento = jsonObj.GetProperty("TipoMovimiento").GetString() ?? "debito",
                NumeroComprobante = jsonObj.GetProperty("NumeroComprobante").GetInt64(),
                FechaMovimiento = jsonObj.GetProperty("FechaMovimiento").GetDateTime().Date,
                ConnectionStringBanksys = jsonObj.TryGetProperty("ConnectionStringBanksys", out var connStr) ? connStr.GetString() : null
            };
            if (jsonObj.TryGetProperty("Contrasiento", out var contrasiento) && !contrasiento.ValueKind.Equals(System.Text.Json.JsonValueKind.Null))
                solicitud.Contrasiento = contrasiento.GetString();
            if (string.IsNullOrWhiteSpace(solicitud.TipoMovimiento) || solicitud.NumeroCuenta == 0 || solicitud.Importe <= 0)
                return null;
            return solicitud;
        }
        catch
        {
            return null;
        }
    }

    private string ObtenerConnectionStringSegunSolicitud(SolicitudRabbitDto solicitud)
    {
        if (!string.IsNullOrEmpty(solicitud.ConnectionStringBanksys) &&
            !solicitud.ConnectionStringBanksys.Contains("CONNECTION_STRING") &&
            !solicitud.ConnectionStringBanksys.Contains("<") &&
            !solicitud.ConnectionStringBanksys.Contains(">") &&
            solicitud.ConnectionStringBanksys.Contains("Server=") &&
            solicitud.ConnectionStringBanksys.Contains("Database="))
        {
            _logger.LogInformation("Usando cadena de conexión proporcionada en el mensaje");
            return solicitud.ConnectionStringBanksys;
        }
        _logger.LogInformation("Usando cadena de conexión de la configuración local");
        return ObtenerConnectionString();
    }

    private OperacionResultado EjecutarMovimiento(SolicitudRabbitDto solicitud, string connectionString)
    {
        var resultado = new OperacionResultado();
        try
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    if (solicitud.TipoMovimiento == "debito" || solicitud.TipoMovimiento == "contrasiento_debito")
                        cmd.CommandText = "sp_DebitarCuenta";
                    else if (solicitud.TipoMovimiento == "credito" || solicitud.TipoMovimiento == "contrasiento_credito")
                        cmd.CommandText = "sp_CreditarCuenta";
                    else
                    {
                        resultado.Exito = false;
                        resultado.MensajeError = $"Tipo de movimiento no soportado: {solicitud.TipoMovimiento}";
                        return resultado;
                    }
                    cmd.Parameters.AddWithValue("@NumeroCuenta", solicitud.NumeroCuenta);
                    cmd.Parameters.AddWithValue("@Importe", solicitud.Importe);
                    cmd.Parameters.AddWithValue("@FechaMovimiento", solicitud.FechaMovimiento);
                    cmd.Parameters.AddWithValue("@NumeroComprobante", solicitud.NumeroComprobante == 0 ? (object)DBNull.Value : solicitud.NumeroComprobante);
                    cmd.Parameters.AddWithValue("@Contrasiento", solicitud.Contrasiento ?? (object)DBNull.Value);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read() && reader["SaldoFinal"] != DBNull.Value)
                        {
                            resultado.Exito = true;
                            resultado.SaldoFinal = reader.GetDecimal(reader.GetOrdinal("SaldoFinal"));
                        }
                        else
                        {
                            resultado.Exito = true;
                            resultado.SaldoFinal = null;
                        }
                    }
                }
            }
        }
        catch (SqlException sqlEx)
        {
            resultado.Exito = false;
            resultado.MensajeError = $"Error SQL ({sqlEx.Number}): {sqlEx.Message}";
        }
        catch (Exception ex)
        {
            resultado.Exito = false;
            resultado.MensajeError = $"Error inesperado: {ex.Message}";
        }
        return resultado;
    }

    private class OperacionResultado
    {
        public bool Exito { get; set; }
        public decimal? SaldoFinal { get; set; }
        public string? MensajeError { get; set; }
    }

    private string ObtenerConnectionString()
    {
        // Buscar primero con la clave de ADO (BanksysConnection)
        var connStr = _configuration.GetConnectionString("BanksysConnection");
        
        // Si no se encuentra, intentar con la clave ConnectionStringBanksys
        if (string.IsNullOrWhiteSpace(connStr))
        {
            connStr = _configuration["ConnectionStringBanksys"];
        }
        
        // Si aún no se encuentra, buscar en ConnectionStrings:Banksys
        if (string.IsNullOrWhiteSpace(connStr))
        {
            connStr = _configuration["ConnectionStrings:Banksys"];
        }
        
        // Si no se encuentra en ninguna clave, lanzar error
        if (string.IsNullOrWhiteSpace(connStr))
        {
            _logger.LogError("No se encontró la cadena de conexión 'BanksysConnection' en la configuración.");
            throw new InvalidOperationException("Cadena de conexión no configurada");
        }
        
        return connStr;
    }

    private string ObtenerNombreCola()
    {
        var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
        var configName = "";
        var args = Environment.GetCommandLineArgs();
        foreach (var arg in args)
        {
            if (arg.Contains("appsettings.cola_"))
                configName = arg;
        }
        if (string.IsNullOrEmpty(configName))
            configName = "appsettings.cola_1.json";
        var match = System.Text.RegularExpressions.Regex.Match(configName, @"cola_(\d+)");
        return match.Success ? $"cola_{match.Groups[1].Value}" : "cola_1";
    }

    private void ActualizarSaldoHandler(long numeroCuenta, decimal saldoFinal, IServiceProvider serviceProvider)
    {
        if (serviceProvider is null)
        {
            _logger.LogError("ServiceProviderAccessor.Instance es null. No se puede actualizar saldo en Handler.");
            return;
        }
        using (var scope = serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HandlerDbContext>();
            var cuenta = db.Cuentas.FirstOrDefault(c => c.Numero == numeroCuenta);
            if (cuenta != null)
            {
                cuenta.Saldo = saldoFinal;
                db.SaveChanges();
                _logger.LogInformation($"Saldo actualizado en Handler para cuenta {numeroCuenta}: {saldoFinal}");
            }
            else
            {
                _logger.LogWarning($"No se encontró la cuenta {numeroCuenta} en Handler para actualizar saldo.");
            }
        }
    }

    private void ActualizarSolicitudHandler(int solicitudId, decimal saldoFinal, IServiceProvider serviceProvider)
    {
        if (serviceProvider is null)
        {
            _logger.LogError("ServiceProviderAccessor.Instance es null. No se puede actualizar solicitud en Handler.");
            return;
        }
        using (var scope = serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HandlerDbContext>();
            var solicitud = db.SolicitudesDebito.FirstOrDefault(s => s.Id == solicitudId);
            if (solicitud != null)
            {
                solicitud.SaldoRespuesta = saldoFinal;
                solicitud.Estado = "actualizada";
                solicitud.FechaReal = DateTime.Now;
                db.SaveChanges();
                _logger.LogInformation($"Solicitud actualizada en Handler para Id {solicitudId} con saldo {saldoFinal}");
            }
            else
            {
                _logger.LogWarning($"No se encontró solicitud con Id {solicitudId} en Handler.");
            }
        }
    }
}
