using Handler.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Handler.Services;

var builder = WebApplication.CreateBuilder(args);

// Cargar configuración JWT
builder.Configuration.AddJsonFile("appsettings.jwt.json", optional: false, reloadOnChange: true);


// Leer RabbitConfig.json para la configuración de RabbitMQ y colas
var rabbitConfigPath = builder.Configuration["ConfigPath"] ?? "Handler/Config/RabbitConfig.json";
var rabbitConfigJson = File.ReadAllText(rabbitConfigPath);
var rabbitConfig = System.Text.Json.JsonSerializer.Deserialize<Handler.Services.RabbitConfig>(rabbitConfigJson) ?? new Handler.Services.RabbitConfig();
builder.Services.AddSingleton<Handler.Services.RabbitConfig>(rabbitConfig);
builder.Services.AddSingleton<Handler.Services.IRabbitConfigService>(sp =>
    new Handler.Services.RabbitConfigService(rabbitConfig)
);
builder.Services.AddSingleton<RabbitMQ.Client.IConnection>(sp =>
{
    var config = sp.GetRequiredService<Handler.Services.RabbitConfig>();
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

// Configuración de autenticación JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = jwtSettings["Key"];
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];

builder.Services.AddAuthentication(options =>
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key ?? string.Empty))
        };
    });

// Servicios
builder.Services.AddControllers();
// Configuración de CORS por defecto
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Handler API", Version = "v1" });
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
builder.Services.AddSingleton<RabbitMqPublisher>(sp =>
{
    var configService = sp.GetRequiredService<IRabbitConfigService>();
    var connection = sp.GetRequiredService<RabbitMQ.Client.IConnection>();
    return new RabbitMqPublisher(configService, connection);
});
builder.Services.AddDbContext<HandlerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Servicio de estado del Handler
builder.Services.AddSingleton<IHandlerStatusService, HandlerStatusService>();
// Servicio de configuración de RabbitMQ
builder.Services.AddSingleton<IRabbitConfigService, RabbitConfigService>();
// Servicio de publicación RabbitMQ
builder.Services.AddSingleton<RabbitMqPublisher>();
// Servicio de estadísticas (scoped)
builder.Services.AddScoped<IEstadisticaService, EstadisticaService>();

// Servicio de saldos (scoped)
builder.Services.AddScoped<ISaldoService, SaldoService>();
// Servicio de inicialización de cuentas (scoped)
builder.Services.AddScoped<ICuentaInitService, CuentaInitService>();
// Servicio de inicialización de cuentas Banksys (scoped)
builder.Services.AddScoped<ICuentaBanksysInitService, CuentaBanksysInitService>();
// Servicio de autenticación JWT
builder.Services.AddSingleton<IAuthService, AuthService>();

// Servicio de solicitudes (scoped)
builder.Services.AddScoped<ISolicitudService, SolicitudService>(sp =>
    new Handler.Services.SolicitudService(
        sp.GetRequiredService<HandlerDbContext>(),
        sp.GetRequiredService<RabbitMqPublisher>(),
        sp.GetRequiredService<IRabbitConfigService>(),
        sp.GetRequiredService<IHandlerStatusService>()
    )
);

// Servicios adicionales
builder.Services.AddSingleton<IConfigColasService, ConfigColasService>();
builder.Services.AddSingleton<IAuthService, AuthService>();

var jwtConfig = builder.Configuration.GetSection("Jwt");
// Solo una configuración JWT
builder.Services.AddAuthorization();

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Handler API v1");
        c.RoutePrefix = string.Empty;
    });
}

// Activar CORS antes de los middlewares principales
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
// Necesario para pruebas de integración
namespace Handler
{
    public partial class Program { }
}
