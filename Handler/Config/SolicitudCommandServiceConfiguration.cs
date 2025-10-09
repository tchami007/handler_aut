// Ejemplo de configuración en Program.cs o Startup.cs

using Handler.Services;

namespace Handler.Config
{
    public static class ServiceConfiguration
    {
        /// <summary>
        /// Configuración para usar el servicio con actualización de saldo en background.
        /// Respuesta rápida, saldo se actualiza de manera diferida.
        /// </summary>
        public static void ConfigureBackgroundSaldoUpdate(this IServiceCollection services)
        {
            // Registrar la implementación que actualiza saldo en background
            services.AddSingleton<ISolicitudCommandQueueService, SolicitudCommandQueueBackgroundService>();
        }

        /// <summary>
        /// Configuración para usar el servicio con actualización de saldo inmediata.
        /// Respuesta con saldo actualizado, transacción serializable.
        /// </summary>
        public static void ConfigureInmediateSaldoUpdate(this IServiceCollection services)
        {
            // Registrar la implementación que actualiza saldo inmediatamente
            services.AddSingleton<ISolicitudCommandQueueService, SolicitudCommandQueueInmediateService>();
        }

        /// <summary>
        /// Configuración basada en parámetro de configuración.
        /// </summary>
        public static void ConfigureSolicitudCommandService(this IServiceCollection services, IConfiguration configuration)
        {
            var useInmediateUpdate = configuration.GetValue<bool>("SolicitudCommand:UseInmediateUpdate", false);
            
            if (useInmediateUpdate)
            {
                Console.WriteLine("[Config] Configurando SolicitudCommandQueueInmediateService (actualización inmediata de saldo)");
                services.AddSingleton<ISolicitudCommandQueueService, SolicitudCommandQueueInmediateService>();
            }
            else
            {
                Console.WriteLine("[Config] Configurando SolicitudCommandQueueBackgroundService (actualización diferida de saldo)");
                services.AddSingleton<ISolicitudCommandQueueService, SolicitudCommandQueueBackgroundService>();
            }
        }
    }
}

// Ejemplo de uso en Program.cs:
/*
var builder = WebApplication.CreateBuilder(args);

// Opción 1: Configuración específica
builder.Services.ConfigureBackgroundSaldoUpdate();

// Opción 2: Configuración específica
builder.Services.ConfigureInmediateSaldoUpdate();

// Opción 3: Configuración basada en appsettings.json
builder.Services.ConfigureSolicitudCommandService(builder.Configuration);

var app = builder.Build();
*/

// Ejemplo de configuración en appsettings.json:
/*
{
  "SolicitudCommand": {
    "UseInmediateUpdate": false  // true para actualización inmediata, false para background
  }
}
*/