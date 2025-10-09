# Servicios de Cola de Comandos de Solicitudes

## Arquitectura

Se han creado dos implementaciones intercambiables del servicio de cola de comandos, cada una con un enfoque diferente para el manejo de saldos:

### 1. SolicitudCommandQueueBackgroundService
**Actualización de saldo en background**

- ✅ **Respuesta ultra-rápida**: Solo validaciones básicas
- ✅ **Saldo diferido**: Se actualiza en background de manera serializada
- ✅ **Alto rendimiento**: Ideal para alta concurrencia
- ✅ **Eventual consistency**: El saldo se actualiza poco después

**Flujo:**
```
1. Validaciones rápidas → 2. Respuesta inmediata → 3. Cola → 4. Actualización saldo + RabbitMQ
```

### 2. SolicitudCommandQueueInmediateService
**Actualización de saldo inmediata**

- ✅ **Consistencia inmediata**: Saldo actualizado antes de responder
- ✅ **Transacción serializable**: Máximo nivel de aislamiento
- ✅ **Reintentos inteligentes**: Manejo robusto de bloqueos
- ✅ **Strong consistency**: Saldo siempre correcto en la respuesta

**Flujo:**
```
1. Validaciones + Actualización saldo (TX serializable) → 2. Respuesta con saldo actualizado → 3. Cola → 4. Solo registro + RabbitMQ
```

## Interfaz Común

Ambas implementaciones siguen la interfaz `ISolicitudCommandQueueService`:

```csharp
public interface ISolicitudCommandQueueService
{
    SolicitudResultadoDto EncolarSolicitud(RegistroSolicitudDto dto);
}
```

## Intercambio de Implementaciones

### Desde el Controller
El controller solo depende de la interfaz:

```csharp
public class SolicitudCommandController : ControllerBase
{
    private readonly ISolicitudCommandQueueService _commandQueueService;

    public SolicitudCommandController(ISolicitudCommandQueueService commandQueueService)
    {
        _commandQueueService = commandQueueService;
    }
}
```

### Configuración en Program.cs

#### Opción 1: Background Update
```csharp
builder.Services.AddSingleton<ISolicitudCommandQueueService, SolicitudCommandQueueBackgroundService>();
```

#### Opción 2: Immediate Update
```csharp
builder.Services.AddSingleton<ISolicitudCommandQueueService, SolicitudCommandQueueInmediateService>();
```

#### Opción 3: Configuración Dinámica
```csharp
builder.Services.ConfigureSolicitudCommandService(builder.Configuration);
```

Con configuración en `appsettings.json`:
```json
{
  "SolicitudCommand": {
    "UseInmediateUpdate": false
  }
}
```

## Cuándo Usar Cada Una

### Use SolicitudCommandQueueBackgroundService cuando:
- ✅ Necesite máximo rendimiento
- ✅ Pueda tolerar eventual consistency
- ✅ Tenga alta concurrencia
- ✅ El saldo en la respuesta no sea crítico

### Use SolicitudCommandQueueInmediateService cuando:
- ✅ Necesite consistencia inmediata
- ✅ El saldo en la respuesta sea crítico
- ✅ Pueda tolerar mayor latencia
- ✅ Requiera transacciones ACID completas

## Características Técnicas

### Ambas Implementaciones Incluyen:
- ✅ Cola por partición (cuenta % número_colas)
- ✅ Procesamiento serializado por cuenta
- ✅ Manejo robusto de excepciones
- ✅ Reintentos con backoff aleatorio
- ✅ Logging detallado
- ✅ Publicación en RabbitMQ
- ✅ Control de estado del handler

### Diferencias Clave:
| Característica | Background | Immediate |
|---|---|---|
| **Actualización saldo** | En cola background | Inmediata |
| **Aislamiento TX** | Default | Serializable |
| **Latencia respuesta** | Ultra-baja | Baja-media |
| **Consistency** | Eventual | Strong |
| **Throughput** | Muy alto | Alto |

## Migración

Para cambiar de implementación:

1. Modificar la configuración en `Program.cs`
2. Reiniciar la aplicación
3. No requiere cambios en controllers o DTOs

El cambio es completamente transparente para el resto del sistema.