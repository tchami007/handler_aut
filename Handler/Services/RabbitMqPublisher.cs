using RabbitMQ.Client;
using System.Text;

namespace Handler.Services
{
    public class RabbitMqPublisher
    {
        private readonly IRabbitConfigService _configService;
        private readonly IConnection _connection;
        private readonly string _exchange;

        public RabbitMqPublisher(IRabbitConfigService configService, IConnection connection)
        {
            _configService = configService;
            _connection = connection;
            _exchange = _configService.GetConfig().Exchange;
        }

        public void Publish(string message, string queue)
        {
            using var channel = _connection.CreateModel();
            channel.ExchangeDeclare(_exchange, ExchangeType.Direct, durable: true);
            channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);
            channel.QueueBind(queue, _exchange, routingKey: queue);
            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: _exchange, routingKey: queue, basicProperties: null, body: body);
        }
    }
}
