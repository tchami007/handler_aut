using Handler.Infrastructure;
using Handler.Services;
using Handler.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Handler.Extensions
{
    /// <summary>
    /// Extensiones para registrar los servicios compartidos de procesamiento de solicitudes.
    /// </summary>
    public static class SharedServicesExtensions
    {
        /// <summary>
        /// Registra todos los servicios compartidos para el procesamiento de solicitudes.
        /// </summary>
        /// <param name="services">Colección de servicios</param>
        /// <param name="cantidadColas">Cantidad de colas para distribución (por defecto se obtiene del config)</param>
        /// <returns>Colección de servicios para chaining</returns>
        public static IServiceCollection AddSolicitudSharedServices(
            this IServiceCollection services, 
            int? cantidadColas = null)
        {
            // Servicios core
            services.AddTransient<SolicitudValidationService>();
            services.AddTransient<SaldoCalculationService>();
            services.AddTransient<RabbitMqMessageService>();
            services.AddTransient<DatabaseRetryService>();
            services.AddTransient<OptimizedTransactionService>();
            services.AddTransient<SolicitudProcessingService>();

            // Configuración de retry
            services.AddSingleton(new RetryConfiguration
            {
                CantidadReintentos = 10,
                TiempoMinimoEsperaMs = 50,
                TiempoMaximoEsperaMs = 100
            });

            // ColaDistributionService necesita cantidad de colas
            if (cantidadColas.HasValue)
            {
                services.AddSingleton(serviceProvider =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<ColaDistributionService>>();
                    return new ColaDistributionService(cantidadColas.Value, logger);
                });
            }
            else
            {
                services.AddSingleton(serviceProvider =>
                {
                    // Obtener cantidad de colas desde configuración
                    var configService = serviceProvider.GetRequiredService<IRabbitConfigService>();
                    var config = configService.GetConfig();
                    int colasCount = config.Colas?.Count ?? 1;
                    
                    var logger = serviceProvider.GetRequiredService<ILogger<ColaDistributionService>>();
                    return new ColaDistributionService(colasCount, logger);
                });
            }

            return services;
        }
    }
}