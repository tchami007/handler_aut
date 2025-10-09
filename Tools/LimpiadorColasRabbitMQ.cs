using RabbitMQ.Client;
using System.Text.Json;

namespace Handler.Tools
{
    /// <summary>
    /// Herramienta para limpiar colas de RabbitMQ no utilizadas (cola_11 a cola_18)
    /// Utiliza la configuración existente del proyecto
    /// </summary>
    public class LimpiadorColasRabbitMQ
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _userName;
        private readonly string _password;
        private readonly string _virtualHost;
        private readonly bool _dryRun;

        public LimpiadorColasRabbitMQ(string host = "172.16.57.184", int port = 5672, 
            string userName = "prueba", string password = "Censys2300*", 
            string virtualHost = "/", bool dryRun = false)
        {
            _host = host;
            _port = port;
            _userName = userName;
            _password = password;
            _virtualHost = virtualHost;
            _dryRun = dryRun;
        }

        /// <summary>
        /// Ejecuta la limpieza de colas de la 11 a la 18
        /// </summary>
        public async Task EjecutarLimpiezaAsync()
        {
            Console.WriteLine("🐰 Iniciando limpieza de colas RabbitMQ");
            Console.WriteLine("=====================================");
            Console.WriteLine();

            var factory = new ConnectionFactory
            {
                HostName = _host,
                Port = _port,
                UserName = _userName,
                Password = _password,
                VirtualHost = _virtualHost
            };

            try
            {
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                Console.WriteLine($"✅ Conectado a RabbitMQ: {_host}:{_port}");
                Console.WriteLine();

                var colasAProcesar = GenerarNombresColas(11, 18);
                var resultados = new List<ResultadoLimpieza>();

                foreach (var nombreCola in colasAProcesar)
                {
                    var resultado = await ProcesarColaAsync(channel, nombreCola);
                    resultados.Add(resultado);
                }

                MostrarResumen(resultados);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error de conexión: {ex.Message}");
                Console.WriteLine("💡 Verifica que RabbitMQ esté ejecutándose y las credenciales sean correctas");
            }
        }

        /// <summary>
        /// Procesa una cola individual (purga y elimina)
        /// </summary>
        private async Task<ResultadoLimpieza> ProcesarColaAsync(IModel channel, string nombreCola)
        {
            var resultado = new ResultadoLimpieza { NombreCola = nombreCola };

            try
            {
                Console.WriteLine($"🔍 Procesando cola: {nombreCola}");

                // Verificar si la cola existe usando QueueDeclarePassive
                uint messageCount = 0;
                uint consumerCount = 0;
                bool colaExiste = true;

                try
                {
                    var queueDeclareOk = channel.QueueDeclarePassive(nombreCola);
                    messageCount = queueDeclareOk.MessageCount;
                    consumerCount = queueDeclareOk.ConsumerCount;
                    Console.WriteLine($"   📊 Mensajes: {messageCount}, Consumidores: {consumerCount}");
                    resultado.MensajesEncontrados = (int)messageCount;
                    resultado.ConsumidoresActivos = (int)consumerCount;
                }
                catch (RabbitMQ.Client.Exceptions.OperationInterruptedException)
                {
                    // La cola no existe
                    colaExiste = false;
                    Console.WriteLine($"   ℹ️  Cola '{nombreCola}' no existe");
                }

                if (!colaExiste)
                {
                    resultado.Estado = EstadoLimpieza.NoExiste;
                    return resultado;
                }

                resultado.ColaExistia = true;

                if (_dryRun)
                {
                    Console.WriteLine($"   🔄 [DRY RUN] Se eliminaría la cola '{nombreCola}'");
                    resultado.Estado = EstadoLimpieza.SimuladoExitoso;
                    return resultado;
                }

                // Purgar mensajes si los hay
                if (messageCount > 0)
                {
                    Console.WriteLine($"   🧹 Purgando {messageCount} mensajes...");
                    var purgeResult = channel.QueuePurge(nombreCola);
                    Console.WriteLine($"   ✅ {purgeResult} mensajes purgados");
                    resultado.MensajesPurgados = (int)purgeResult;
                }

                // Eliminar la cola
                Console.WriteLine($"   🗑️  Eliminando cola...");
                var deleteResult = channel.QueueDelete(nombreCola);
                Console.WriteLine($"   ✅ Cola '{nombreCola}' eliminada exitosamente");
                
                resultado.Estado = EstadoLimpieza.EliminadaExitosa;
                resultado.MensajesEliminados = (int)deleteResult;

                return resultado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error procesando cola '{nombreCola}': {ex.Message}");
                resultado.Estado = EstadoLimpieza.Error;
                resultado.MensajeError = ex.Message;
                return resultado;
            }
            finally
            {
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Genera lista de nombres de colas en rango específico
        /// </summary>
        private static List<string> GenerarNombresColas(int desde, int hasta)
        {
            var colas = new List<string>();
            for (int i = desde; i <= hasta; i++)
            {
                colas.Add($"cola_{i}");
            }
            return colas;
        }

        /// <summary>
        /// Muestra resumen final de operaciones
        /// </summary>
        private void MostrarResumen(List<ResultadoLimpieza> resultados)
        {
            Console.WriteLine("📋 RESUMEN DE OPERACIONES");
            Console.WriteLine("=========================");
            
            var totalProcesadas = resultados.Count;
            var colasEncontradas = resultados.Where(r => r.ColaExistia).ToList();
            var colasEliminadas = resultados.Where(r => r.Estado == EstadoLimpieza.EliminadaExitosa).ToList();
            var colasConError = resultados.Where(r => r.Estado == EstadoLimpieza.Error).ToList();
            var colasNoEncontradas = resultados.Where(r => r.Estado == EstadoLimpieza.NoExiste).ToList();

            Console.WriteLine($"🔍 Colas procesadas: {totalProcesadas}");
            Console.WriteLine($"✅ Colas encontradas: {colasEncontradas.Count}");
            Console.WriteLine($"❌ Colas no encontradas: {colasNoEncontradas.Count}");

            if (_dryRun)
            {
                Console.WriteLine($"🔄 Modo DRY RUN - Ninguna cola fue modificada");
            }
            else
            {
                Console.WriteLine($"🗑️  Colas eliminadas: {colasEliminadas.Count}");
                Console.WriteLine($"⚠️  Colas con errores: {colasConError.Count}");
            }

            var totalMensajesPurgados = colasEliminadas.Sum(r => r.MensajesPurgados);
            if (totalMensajesPurgados > 0)
            {
                Console.WriteLine($"🧹 Total mensajes purgados: {totalMensajesPurgados}");
            }

            if (colasEncontradas.Any())
            {
                Console.WriteLine();
                Console.WriteLine("📝 Detalle de colas encontradas:");
                foreach (var resultado in colasEncontradas)
                {
                    var icono = resultado.Estado switch
                    {
                        EstadoLimpieza.EliminadaExitosa => "✅",
                        EstadoLimpieza.SimuladoExitoso => "🔄",
                        EstadoLimpieza.Error => "❌",
                        _ => "❓"
                    };
                    
                    var estado = resultado.Estado switch
                    {
                        EstadoLimpieza.EliminadaExitosa => "ELIMINADA",
                        EstadoLimpieza.SimuladoExitoso => "SIMULADO",
                        EstadoLimpieza.Error => $"ERROR: {resultado.MensajeError}",
                        _ => "DESCONOCIDO"
                    };

                    Console.WriteLine($"   {icono} {resultado.NombreCola}: {estado}");
                    if (resultado.MensajesEncontrados > 0)
                    {
                        Console.WriteLine($"      📊 Mensajes: {resultado.MensajesEncontrados}");
                    }
                }
            }

            Console.WriteLine();
            if (_dryRun)
            {
                Console.WriteLine("💡 Para ejecutar realmente, crea una instancia con dryRun=false");
            }
            else
            {
                Console.WriteLine("🎉 Operación completada");
            }
        }
    }

    /// <summary>
    /// Resultado del procesamiento de una cola individual
    /// </summary>
    public class ResultadoLimpieza
    {
        public string NombreCola { get; set; } = string.Empty;
        public bool ColaExistia { get; set; }
        public int MensajesEncontrados { get; set; }
        public int ConsumidoresActivos { get; set; }
        public int MensajesPurgados { get; set; }
        public int MensajesEliminados { get; set; }
        public EstadoLimpieza Estado { get; set; }
        public string? MensajeError { get; set; }
    }

    /// <summary>
    /// Estado del procesamiento de una cola
    /// </summary>
    public enum EstadoLimpieza
    {
        NoExiste,
        EliminadaExitosa,
        SimuladoExitoso,
        Error
    }

    /// <summary>
    /// Programa principal para ejecutar la herramienta
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            bool dryRun = args.Contains("--dry-run") || args.Contains("-d");
            
            Console.WriteLine("🛠️  Herramienta de Limpieza de Colas RabbitMQ");
            Console.WriteLine("===============================================");
            Console.WriteLine();
            
            if (dryRun)
            {
                Console.WriteLine("🔄 Modo DRY RUN - Solo simulación, no se realizarán cambios");
                Console.WriteLine();
            }

            var limpiador = new LimpiadorColasRabbitMQ(dryRun: dryRun);
            await limpiador.EjecutarLimpiezaAsync();

            Console.WriteLine();
            Console.WriteLine("Presiona cualquier tecla para continuar...");
            Console.ReadKey();
        }
    }
}