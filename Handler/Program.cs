using Handler.Extensions;
using Serilog;

// Configurar Serilog primero
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile("appsettings.jwt.json", optional: false)
        .Build())
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Usar Serilog para logging
    builder.Host.UseSerilog();

    // Cargar configuración JWT adicional
    builder.Configuration.AddJsonFile("appsettings.jwt.json", optional: false, reloadOnChange: true);

    Log.Information("🚀 Iniciando configuración de Handler API...");

    // Configurar servicios de manera organizada
    builder.Services.AddControllers();
    builder.Services.AddDatabaseServices(builder.Configuration);
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddRabbitMqServices(builder.Configuration);
    builder.Services.AddBusinessServices();
    builder.Services.AddSolicitudSharedServices(); // Agregar servicios compartidos
    builder.Services.AddSolicitudCommandService(builder.Configuration);
    builder.Services.AddCorsConfiguration(builder.Configuration);
    builder.Services.AddSwaggerConfiguration();

    Log.Information("✅ Servicios configurados correctamente");

    var app = builder.Build();

    // Pipeline de middleware
    if (app.Environment.IsDevelopment())
    {
        Log.Information("🔧 Modo desarrollo: Habilitando Swagger UI");
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Handler API v1");
            c.RoutePrefix = string.Empty; // Swagger en la raíz
        });
    }

    // Configurar pipeline en orden correcto
    app.UseCors("AllowAll");
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("🎯 Handler API configurado y listo para ejecutar");

    // Log de URLs configuradas
    var configuredUrls = app.Configuration["urls"];
    if (!string.IsNullOrEmpty(configuredUrls))
    {
        Log.Information("🌐 URLs configuradas: {URLs}", configuredUrls);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Error crítico durante el inicio de la aplicación");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Necesario para pruebas de integración
namespace Handler
{
    public partial class Program { }
}