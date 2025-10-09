using Handler.Infrastructure;
using Handler.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Serilog;

namespace Handler.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configura los servicios de base de datos
        /// </summary>
        public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var banksysConnectionString = configuration.GetConnectionString("BanksysConnection");
            
            // Log de configuración sin datos sensibles
            Log.Information("🗄️ Configurando base de datos - Servidor: {Servidor}", 
                ExtractServerName(connectionString));
            Log.Information("🗄️ Configurando Banksys - Servidor: {Servidor}", 
                ExtractServerName(banksysConnectionString));
            
            // DbContext sin estrategia de reintento automático (incompatible con transacciones manuales)
            services.AddDbContext<HandlerDbContext>(options =>
                options.UseSqlServer(connectionString));
            
            // Repositories
            services.AddScoped<ICuentaRepository, CuentaRepository>();
            services.AddScoped<ISolicitudRepository, SolicitudRepository>();
            services.AddScoped<ILogOperacionRepository, LogOperacionRepository>();
            
            return services;
        }

        private static string ExtractServerName(string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) return "No configurado";
            
            var parts = connectionString.Split(';');
            var serverPart = parts.FirstOrDefault(p => p.Trim().StartsWith("Server=", StringComparison.OrdinalIgnoreCase));
            return serverPart?.Split('=')[1] ?? "Desconocido";
        }

        /// <summary>
        /// Configura los servicios de autenticación JWT
        /// </summary>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("Jwt");
            var key = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is required");
            var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is required");
            var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is required");

            // Log de configuración JWT (sin mostrar la clave secreta)
            Log.Information("🔐 Configurando JWT - Issuer: {Issuer}, Audience: {Audience}, Key: {KeyInfo}", 
                issuer, 
                audience, 
                $"[{key.Length} caracteres]");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
                };
            });

            services.AddAuthorization();
            return services;
        }

        /// <summary>
        /// Configura los servicios de RabbitMQ
        /// </summary>
        public static IServiceCollection AddRabbitMqServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Leer configuración de RabbitMQ de manera segura
            var rabbitConfigPath = configuration["ConfigPath"] ?? "Handler/Config/RabbitConfig.json";
            
            if (!File.Exists(rabbitConfigPath))
            {
                throw new FileNotFoundException($"RabbitMQ configuration file not found: {rabbitConfigPath}");
            }

            var rabbitConfigJson = File.ReadAllText(rabbitConfigPath);
            var rabbitConfig = System.Text.Json.JsonSerializer.Deserialize<RabbitConfig>(rabbitConfigJson) 
                ?? throw new InvalidOperationException("Failed to deserialize RabbitMQ configuration");

            // Log de configuración de RabbitMQ (sin datos sensibles)
            Log.Information("🐰 Configurando RabbitMQ - Host: {Host}:{Port}, VHost: {VirtualHost}, Usuario: {UserName}, Colas: {CantidadColas}", 
                rabbitConfig.Host, 
                rabbitConfig.Port, 
                rabbitConfig.VirtualHost, 
                rabbitConfig.UserName, 
                rabbitConfig.Colas?.Count ?? 0);

            // Registro de servicios RabbitMQ
            services.AddSingleton(rabbitConfig);
            services.AddSingleton<IRabbitConfigService>(sp => new RabbitConfigService(rabbitConfig));
            
            services.AddSingleton<RabbitMQ.Client.IConnection>(sp =>
            {
                var config = sp.GetRequiredService<RabbitConfig>();
                var factory = new RabbitMQ.Client.ConnectionFactory
                {
                    HostName = config.Host,
                    Port = config.Port,
                    UserName = config.UserName,
                    Password = config.Password,
                    VirtualHost = config.VirtualHost
                };
                return factory.CreateConnection();
            });

            services.AddSingleton<RabbitMqPublisher>();
            return services;
        }

        /// <summary>
        /// Configura los servicios de negocio principales
        /// </summary>
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            // Servicios Singleton
            services.AddSingleton<IHandlerStatusService, HandlerStatusService>();
            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<IConfigColasService, ConfigColasService>();

            // Servicios Scoped
            services.AddScoped<IEstadisticaService, EstadisticaService>();
            services.AddScoped<ISaldoService, SaldoService>();
            services.AddScoped<ICuentaInitService, CuentaInitService>();
            services.AddScoped<ICuentaBanksysInitService, CuentaBanksysInitService>();
            services.AddScoped<ISolicitudService, SolicitudService>();

            return services;
        }

        /// <summary>
        /// Configura el servicio de cola de comandos (intercambiable)
        /// </summary>
        public static IServiceCollection AddSolicitudCommandService(this IServiceCollection services, IConfiguration configuration)
        {
            var useInmediateUpdate = configuration.GetValue<bool>("SolicitudCommand:UseInmediateUpdate", true);
            
            if (useInmediateUpdate)
            {
                Log.Information("✅ SolicitudCommandQueueInmediateService (actualización inmediata de saldo)");
                services.AddSingleton<ISolicitudCommandQueueService, SolicitudCommandQueueInmediateService>();
            }
            else
            {
                Log.Information("✅ SolicitudCommandQueueBackgroundService (actualización diferida de saldo)");
                services.AddSingleton<ISolicitudCommandQueueService, SolicitudCommandQueueBackgroundService>();
            }

            return services;
        }

        /// <summary>
        /// Configura CORS de manera segura
        /// </summary>
        public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "*" };
            
            services.AddCors(options =>
            {
                if (allowedOrigins.Contains("*"))
                {
                    // Solo para desarrollo
                    options.AddPolicy("AllowAll", policy => 
                        policy.AllowAnyOrigin()
                              .AllowAnyHeader()
                              .AllowAnyMethod());
                    Log.Warning("⚠️ CORS: Configuración permisiva (solo para desarrollo)");
                }
                else
                {
                    // Para producción
                    options.AddPolicy("AllowAll", policy => 
                        policy.WithOrigins(allowedOrigins)
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials());
                    Log.Information("✅ CORS: Configuración segura para: {AllowedOrigins}", string.Join(", ", allowedOrigins));
                }
            });

            return services;
        }

        /// <summary>
        /// Configura Swagger/OpenAPI
        /// </summary>
        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo 
                { 
                    Title = "Handler API", 
                    Version = "v1",
                    Description = "API para manejo de solicitudes de débito/crédito con cola de comandos"
                });
                
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Ingrese el token JWT en el campo: Bearer {token}"
                });
                
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });

            return services;
        }
    }
}