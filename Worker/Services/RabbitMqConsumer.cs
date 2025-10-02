using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Worker.Services
{
    public class RabbitMqConsumer
    {
    private readonly RabbitConfig _config;
        private readonly string _queue;

        public RabbitMqConsumer(string queue)
        {
            // Leer la ruta desde appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            var configRoot = builder.Build();
            var configPath = configRoot["ConfigPath"];
            if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
                throw new Exception($"No se encontró la configuración centralizada: {configPath}");
            var json = File.ReadAllText(configPath);
            _config = System.Text.Json.JsonSerializer.Deserialize<RabbitConfig>(json) ?? new RabbitConfig();
            _queue = queue;
            Console.WriteLine($"[WORKER] Iniciando worker para la cola: {_queue}");
        }

        public void StartConsuming(Action<string> onMessage)
        {
            var factory = new RabbitMQ.Client.ConnectionFactory
            {
                HostName = _config.Host,
                Port = _config.Port,
                UserName = _config.UserName,
                Password = _config.Password,
                VirtualHost = _config.VirtualHost
            };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            channel.QueueDeclare(_queue, durable: true, exclusive: false, autoDelete: false);
            var consumer = new RabbitMQ.Client.Events.EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = System.Text.Encoding.UTF8.GetString(body);
                Console.WriteLine($"[WORKER:{_queue}] Mensaje recibido: {message}");
                onMessage($"[{_queue}] {message}");
            };
            channel.BasicConsume(queue: _queue, autoAck: true, consumer: consumer);
        }
    }
}
