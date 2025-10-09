# Justificación del Uso del Service Locator Pattern

## Resumen Ejecutivo

En el proyecto Handler de Autorización, se utilizan dos patrones de inyección de dependencias:

1. **Inyección Directa** (recomendado) - Para servicios simples
2. **Service Locator Pattern** (justificado) - Para servicios de cola con requerimientos especiales

## Servicios con Inyección Directa ✅

### SaldoService.cs
```csharp
public SaldoService(ICuentaRepository cuentaRepository)
{
    _cuentaRepository = cuentaRepository;
}
```

### EstadisticaService.cs
```csharp
public EstadisticaService(ISolicitudRepository solicitudRepository, 
                         ICuentaRepository cuentaRepository, 
                         ILogOperacionRepository logRepository,
                         IConnection rabbitConnection, 
                         ILogger<EstadisticaService> logger)
```

### SolicitudService.cs
```csharp
public SolicitudService(ICuentaRepository cuentaRepository,
                       ISolicitudRepository solicitudRepository,
                       HandlerDbContext db,
                       ILogger<SolicitudService> logger)
```

## Servicios con Service Locator Pattern (Justificado) ⚖️

### SolicitudCommandQueueInmediateService.cs
### SolicitudCommandQueueBackgroundService.cs

**¿Por qué usan Service Locator?**

### 1. Arquitectura de Múltiples Colas Concurrentes
```csharp
// Inicialización de N colas concurrentes
for (int i = 1; i <= _cantidadColas; i++)
{
    var channel = Channel.CreateUnbounded<RegistroSolicitudDto>();
    _colas[i] = channel;
    _ = Task.Run(() => ProcesarColaAsync(i, channel)); // <- Aquí empieza el problema
}
```

Cada cola ejecuta en un **Task independiente** que requiere su propio scope de DI.

### 2. Transacciones Serializables con Scopes Independientes
```csharp
public SolicitudResultadoDto EncolarSolicitud(RegistroSolicitudDto dto)
{
    // CADA INVOCACIÓN requiere un scope independiente para transacciones serializables
    using var scope = _serviceProvider.CreateScope();
    
    // Dentro del scope: transacción serializable
    using var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable);
}
```

### 3. Procesamiento Asíncrono Background
```csharp
private async Task ProcesarColaAsync(int particion, Channel<RegistroSolicitudDto> channel)
{
    await foreach (var dto in channel.Reader.ReadAllAsync())
    {
        // Cada procesamiento background requiere scope independiente
        using (var scope = _serviceProvider.CreateScope())
        {
            // Transacción independiente por mensaje
        }
    }
}
```

## Limitaciones de la Inyección Directa en Este Caso

### ❌ Problema 1: Scoped Lifetime Conflict
Si inyectáramos directamente:
```csharp
public SolicitudCommandQueueInmediateService(ICuentaRepository repo, HandlerDbContext db)
```

**Resultado:** Todos los tasks compartirían el mismo DbContext, causando:
- Conflictos de transacciones concurrentes
- Estado compartido entre operaciones que deben ser independientes
- Violación del principio de aislamiento de transacciones serializables

### ❌ Problema 2: Lifetime Management
```csharp
_ = Task.Run(() => ProcesarColaAsync(i, channel));
```

Los **background tasks** viven más tiempo que el scope del servicio padre. Requieren:
- Scope independiente por procesamiento
- Gestión autónoma de lifetime del DbContext
- Transacciones aisladas por mensaje

### ❌ Problema 3: Concurrencia de Alto Rendimiento
Con inyección directa, tendríamos:
- **1 DbContext** compartido entre N colas
- **1 Repository** compartido entre operaciones concurrentes
- **Bloqueos** y conflictos de estado

Con Service Locator tenemos:
- **N DbContexts** independientes (uno por scope)
- **N Repositories** independientes
- **Aislamiento completo** entre operaciones

## Solución Implementada: Service Locator Documentado

### Patrón Aplicado
```csharp
/// <summary>
/// NOTA: Usa Service Locator pattern por necesidad arquitectónica:
/// - Requiere scope independiente para transacciones serializables
/// - Manejo de múltiples colas concurrentes  
/// - Inyección directa no es viable en este contexto asíncrono
/// </summary>
public SolicitudResultadoDto EncolarSolicitud(RegistroSolicitudDto dto)
{
    using var scope = _serviceProvider.CreateScope();
    var cuentaRepository = scope.ServiceProvider.GetRequiredService<ICuentaRepository>();
    var solicitudRepository = scope.ServiceProvider.GetRequiredService<ISolicitudRepository>();
    // ... resto de la implementación
}
```

### Beneficios del Enfoque
1. **Documentación explícita** del por qué se usa Service Locator
2. **Uso de repositorios** dentro de cada scope
3. **Aislamiento transaccional** completo
4. **Rendimiento óptimo** para alta concurrencia

## Alternativas Evaluadas

### 1. Factory Pattern
**Problema:** Agrega complejidad innecesaria sin beneficios reales
```csharp
// Más complejo, mismo resultado
public interface ITransactionalServiceFactory
{
    Task<T> ExecuteInScopeAsync<T>(Func<...> operation);
}
```

### 2. Async Scoped Services
**Problema:** No resuelve el problema fundamental de scopes independientes por task

### 3. Message Bus Pattern
**Problema:** Sobreingeniería para el caso de uso actual

## Conclusión

El uso del **Service Locator pattern** en estos servicios específicos está **arquitectónicamente justificado** debido a:

1. **Requerimientos de concurrencia** únicos
2. **Necesidad de scopes independientes** por transacción
3. **Procesamiento asíncrono** con lifetimes complejos
4. **Rendimiento crítico** en operaciones de alta frecuencia

**El patrón se documenta explícitamente** en el código para:
- Justificar su uso excepcional
- Guiar a futuros desarrolladores
- Mantener arquitectura limpia en el resto del sistema

**Resultado:** Balance entre pragmatismo y principios arquitectónicos, con documentación clara de las decisiones técnicas.

---
*Análisis realizado: Enero 2025*
*Contexto: Refactorización de inyección de dependencias*