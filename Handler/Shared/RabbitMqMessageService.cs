using Handler.Controllers.Dtos;
using Handler.Infrastructure;
using Handler.Models;
using Handler.Services;
using Microsoft.Extensions.Logging;

namespace Handler.Shared
{
    /// <summary>
    /// Servicio para creación y publicación de mensajes en RabbitMQ para solicitudes de débito/crédito.
    /// Centraliza la lógica de formateo y envío de mensajes a las colas.
    /// </summary>
    public class RabbitMqMessageService
    {
        private readonly RabbitMqPublisher _publisher;
        private readonly ILogger<RabbitMqMessageService> _logger;

        public RabbitMqMessageService(RabbitMqPublisher publisher, ILogger<RabbitMqMessageService> logger)
        {
            _publisher = publisher;
            _logger = logger;
        }

        /// <summary>
        /// Crea y publica un mensaje en RabbitMQ basado en una solicitud de débito procesada.
        /// </summary>
        /// <param name="solicitud">Solicitud de débito que se procesó</param>
        /// <param name="dto">DTO original de la solicitud</param>
        /// <param name="nombreCola">Nombre de la cola donde publicar</param>
        public void PublicarSolicitud(SolicitudDebito solicitud, RegistroSolicitudDto dto, string nombreCola)
        {
            try
            {
                var mensajeDto = CrearMensajeRabbit(solicitud, dto);
                var mensaje = System.Text.Json.JsonSerializer.Serialize(mensajeDto);

                _publisher.Publish(mensaje, nombreCola);

                _logger.LogDebug("Mensaje publicado en {NombreCola} para solicitud {SolicitudId} de cuenta {NumeroCuenta}", 
                    nombreCola, solicitud.Id, dto.NumeroCuenta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publicando mensaje en {NombreCola} para solicitud {SolicitudId}", 
                    nombreCola, solicitud.Id);
                throw;
            }
        }

        /// <summary>
        /// Crea un mensaje DTO para RabbitMQ basado en una solicitud procesada.
        /// </summary>
        /// <param name="solicitud">Solicitud de débito procesada</param>
        /// <param name="dto">DTO original de la solicitud</param>
        /// <returns>DTO formateado para RabbitMQ</returns>
        private SolicitudRabbitDto CrearMensajeRabbit(SolicitudDebito solicitud, RegistroSolicitudDto dto)
        {
            return new SolicitudRabbitDto
            {
                Id = solicitud.Id,
                TipoMovimiento = dto.TipoMovimiento,
                Importe = dto.Monto,
                NumeroCuenta = dto.NumeroCuenta,
                FechaMovimiento = DateTime.UtcNow,
                NumeroComprobante = dto.NumeroComprobante,
                Contrasiento = null,
                ConnectionStringBanksys = "<CONNECTION_STRING>"
            };
        }

        /// <summary>
        /// Publica un mensaje usando solo los datos del DTO (sin solicitud persistida).
        /// Útil para casos donde se publica antes de persistir la solicitud.
        /// </summary>
        /// <param name="dto">DTO de la solicitud</param>
        /// <param name="nombreCola">Nombre de la cola donde publicar</param>
        /// <param name="solicitudId">ID de la solicitud (opcional, 0 si no se conoce)</param>
        public void PublicarSolicitudDto(RegistroSolicitudDto dto, string nombreCola, int solicitudId = 0)
        {
            try
            {
                var mensajeDto = new SolicitudRabbitDto
                {
                    Id = solicitudId,
                    TipoMovimiento = dto.TipoMovimiento,
                    Importe = dto.Monto,
                    NumeroCuenta = dto.NumeroCuenta,
                    FechaMovimiento = DateTime.UtcNow,
                    NumeroComprobante = dto.NumeroComprobante,
                    Contrasiento = null,
                    ConnectionStringBanksys = "<CONNECTION_STRING>"
                };

                var mensaje = System.Text.Json.JsonSerializer.Serialize(mensajeDto);
                _publisher.Publish(mensaje, nombreCola);

                _logger.LogDebug("Mensaje DTO publicado en {NombreCola} para solicitud {SolicitudId} de cuenta {NumeroCuenta}", 
                    nombreCola, solicitudId, dto.NumeroCuenta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publicando mensaje DTO en {NombreCola} para cuenta {NumeroCuenta}", 
                    nombreCola, dto.NumeroCuenta);
                throw;
            }
        }
    }
}