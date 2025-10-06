# Guía de Configuración para Handler

Este documento describe los parámetros de configuración requeridos para el correcto funcionamiento del Handler, incluyendo las dos bases de datos, RabbitMQ, colas, JWT y logging. Además, se indica dónde se cargan y utilizan estos valores en el código.

---

## 1. Bases de Datos

**Parámetros en `appsettings.json`:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=...;Database=HandlerAutorizacion;Trusted_Connection=True;TrustServerCertificate=True;",
  "BanksysConnection": "Server=...;Database=Banksys;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

**Flags importantes:**
- `Trusted_Connection=True`: Utiliza la autenticación integrada de Windows para conectarse a SQL Server. El usuario que ejecuta el servicio debe tener permisos en la base de datos.
- `TrustServerCertificate=True`: Permite aceptar el certificado del servidor SQL sin validación estricta. Útil en entornos de desarrollo o pruebas, pero no recomendado en producción sin certificados válidos.

**Dónde se cargan:**
- Archivo: `Handler/Program.cs`
- Líneas relevantes:
  ```csharp
  builder.Services.AddDbContext<HandlerDbContext>(options =>
      options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
  // Para Banksys, busca el uso de "BanksysConnection" en los servicios relacionados.
  ```

**Dónde se utilizan:**
- `HandlerDbContext` para HandlerAutorizacion.
- Servicios que acceden a la base de datos Banksys utilizan la cadena `BanksysConnection`.

---

## 2. RabbitMQ y Colas

**Parámetro en `appsettings.json`:**
```json
"RabbitMQ": {
  "Host": "172.16.57.184",
  "Port": 5672,
  "UserName": "prueba",
  "Password": "Censys2300*",
  "VirtualHost": "/",
  "Exchange": "handler_exchange",
  "CantidadColas": 5,
  "Colas": [
    { "Nombre": "cola_1" },
    { "Nombre": "cola_2" },
    { "Nombre": "cola_3" },
    { "Nombre": "cola_4" },
    { "Nombre": "cola_5" }
  ]
}
```

**Dónde se carga:**
- Archivo: `Handler/Program.cs`
- Líneas relevantes:
  ```csharp
  var rabbitConfig = builder.Configuration.GetSection("RabbitMQ").Get<Handler.Services.RabbitConfig>();
  builder.Services.AddSingleton<Handler.Services.RabbitConfig>(rabbitConfig ?? new Handler.Services.RabbitConfig());
  builder.Services.AddSingleton<Handler.Services.IRabbitConfigService>(sp =>
      new Handler.Services.RabbitConfigService(sp.GetRequiredService<Handler.Services.RabbitConfig>())
  );
  ```

**Dónde se utiliza:**
- Inicialización de la conexión RabbitMQ y publicación de mensajes (`RabbitMqPublisher`, `SolicitudService`).
- Gestión y cálculo de colas para la distribución de mensajes.
- Administración de colas vía API (`ConfigController`, `ConfigColasService`).

---

## 3. JWT (Autenticación)

**Parámetro en `appsettings.json`:**
```json
"Jwt": {
  "Key": "SuperSecretKeyParaJWTHandler2025!",
  "Issuer": "HandlerAut",
  "Audience": "HandlerAutUsers",
  "ExpireMinutes": 60
}
```

**Dónde se carga:**
- Archivo: `Handler/Program.cs`
- Líneas relevantes:
  ```csharp
  var jwtSettings = builder.Configuration.GetSection("Jwt");
  var key = jwtSettings["Key"];
  var issuer = jwtSettings["Issuer"];
  var audience = jwtSettings["Audience"];
  builder.Services.AddAuthentication(...).AddJwtBearer(...);
  builder.Services.AddAuthorization();
  ```

**Dónde se utiliza:**
- En la autenticación y autorización de los endpoints protegidos.
- En la generación y validación de tokens JWT.

---

## 4. Logging

**Parámetro en `appsettings.json`:**
```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.Hosting.Lifetime": "Information"
  }
}
```

**Dónde se carga:**
- Automáticamente por el host de ASP.NET Core al iniciar la aplicación.

**Dónde se utiliza:**
- En todos los servicios y controladores que usan inyección de `ILogger` para registrar información, advertencias y errores.

---

## Resumen de Archivos Clave
- `Handler/Program.cs`: Carga toda la configuración principal.
- `Handler/Services/RabbitConfigService.cs`: Gestiona la configuración de RabbitMQ y colas.
- `Handler/Services/ConfigColasService.cs`: Permite modificar dinámicamente la configuración de colas.
- `Handler/Controllers/ConfigController.cs`: Expone endpoints para consultar y modificar la configuración de colas.
- `Handler/Services/SolicitudService.cs`: Utiliza la configuración de colas para distribuir mensajes.
- `Handler/Controllers/AuthController.cs`: Utiliza la configuración JWT para autenticación.

---

## Recomendaciones
- Verifica que todos los parámetros estén presentes en `appsettings.json` antes de iniciar el Handler.
- Si se modifica la cantidad de colas, actualiza tanto `CantidadColas` como la lista `Colas`.
- Para cambios dinámicos, utiliza los endpoints expuestos por `ConfigController`.
- Mantén las claves y contraseñas seguras y fuera de repositorios públicos.

---

**Última actualización:** 3 de octubre de 2025
