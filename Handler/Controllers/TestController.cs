using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Handler.Controllers.Dtos;

namespace Handler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
        {
            private readonly Services.RabbitMqPublisher _publisher;
            public TestController(Services.RabbitMqPublisher publisher)
            {
                _publisher = publisher;
            }
        [HttpGet("publico")]
        public IActionResult Publico()
        {
            return Ok("Endpoint público, no requiere autenticación.");
        }

    // [Authorize] (removido para pruebas)
        [HttpGet("privado")]
        public IActionResult Privado()
        {
            return Ok($"Endpoint privado, usuario autenticado: {User.Identity?.Name}");
        }
        
            [HttpPost("rabbitmq/enviar")]
            public IActionResult EnviarRabbitMq([FromBody] DtoRabbitMq dto)
            {
                if (string.IsNullOrWhiteSpace(dto.Cola))
                    return BadRequest("Debe especificar el nombre de la cola destino.");
                _publisher.Publish(dto.Mensaje, dto.Cola);
                return Ok($"Mensaje enviado a la cola '{dto.Cola}': {dto.Mensaje}");
            }

            [HttpPost("rabbitmq/test")]
            public IActionResult TestRabbitMq([FromQuery] string mensaje = "Mensaje de prueba", [FromQuery] string routingKey = "test_routing")
            {
                try
                {
                    _publisher.Publish(mensaje, routingKey);
                    return Ok($"Mensaje de prueba enviado a RabbitMQ. RoutingKey: {routingKey}, Mensaje: {mensaje}");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error al enviar mensaje a RabbitMQ: {ex.Message}");
                }
            }
    }
}
