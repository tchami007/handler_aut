# Tareas Detalladas – Etapa 1

## Etapa 1: Configuración de Proyecto y Estructura Base  
**Duración estimada:** 2 días

### Tareas detalladas

1. **Inicialización de la solución**
   - Crear la solución principal en .NET Core 8.
   - Crear los proyectos: Handler (Web API) y Worker (servicio/API).

2. **Configuración de control de versiones**
   - Inicializar repositorio Git.
   - Definir estructura de ramas (main, develop, feature).

3. **Estructuración de carpetas y capas**
   - Crear carpetas: Controller, Service, Infrastructure, Model, Shared en ambos proyectos.
   - Agregar archivos README en cada carpeta para documentación interna.

4. **Configuración de dependencias**
   - Instalar paquetes NuGet necesarios: Entity Framework Core, RabbitMQ.Client, JWT, ADO.net.
   - Configurar archivos de settings (appsettings.json) para conexión a SQL Server y RabbitMQ.

5. **Definición de modelos de datos principales**
   - Crear clases base para cuentas, solicitudes, saldos y logs.
   - Definir entidades y mapeos iniciales para Entity Framework.

6. **Configuración de autenticación**
   - Implementar autenticación JWT básica en el Handler.
   - Configurar middleware de autenticación.

7. **Integración inicial con SQL Server**
   - Configurar cadena de conexión en appsettings.json.
   - Crear migraciones iniciales y base de datos de desarrollo.

8. **Integración inicial con RabbitMQ**
   - Configurar conexión y exchange/queue básicos en el proyecto Handler.
   - Probar envío y recepción de mensajes de prueba.

9. **Configuración de Worker**
   - Implementar estructura base para ejecución en background.
   - Configurar cancelation token y logging básico.

10. **Pruebas de verificación**
    - Ejecutar scripts de test para validar:
      - Creación y consulta de entidades en SQL Server.
      - Envío y recepción de mensajes en RabbitMQ.
      - Autenticación JWT en endpoints protegidos.
      - Estructura y funcionamiento básico de ambos proyectos.

---

Cada tarea puede ser asignada y monitoreada individualmente para asegurar el avance y la calidad en la configuración inicial del proyecto.
