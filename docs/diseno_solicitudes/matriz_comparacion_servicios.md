# Matriz de Comparación - Servicios de Solicitud

## 📊 Comparación Detallada de Servicios

### Características Técnicas

| **Característica** | **SolicitudService** | **BackgroundService** | **InmediateService** |
|-------------------|---------------------|----------------------|---------------------|
| **Patrón** | Secuencial Completo | Validación + Diferido | Saldo Inmediato + Diferido |
| **Transacciones** | ✅ Serializable | ✅ Serializable | ✅ Serializable |
| **Reintentos** | ✅ 10 con aleatorio | ✅ 10 con aleatorio | ✅ 10 con aleatorio |
| **Códigos Error** | ✅ 0-5, 96-99 | ✅ 0-5, 97-99 | ✅ 0-5, 97-99 |
| **RowVersion** | ✅ Soportado | ✅ Soportado | ✅ Soportado |
| **Actualización Saldo** | Inmediata | Diferida | Inmediata |
| **Registro DB** | Inmediato | Diferido | Diferido |
| **RabbitMQ** | Inmediato | Diferido | Diferido |
| **ID Respuesta** | ✅ Retorna ID | ❌ Sin ID | ❌ Sin ID |
| **Latencia** | Alta | Baja | Media |
| **Throughput** | Bajo | Alto | Alto |

### Flujos de Procesamiento

#### SolicitudService (Tradicional)
```
┌─────────────┐   ┌──────────────┐   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐
│ Validaciones│──▶│Update Saldo  │──▶│Registro DB  │──▶│RabbitMQ     │──▶│Respuesta+ID │
└─────────────┘   └──────────────┘   └─────────────┘   └─────────────┘   └─────────────┘
```

#### BackgroundService (Diferido Total)
```
┌─────────────┐   ┌─────────────┐
│Validaciones │──▶│Respuesta    │
│Básicas      │   │Inmediata    │
└─────────────┘   └─────────────┘
      │
      ▼
┌─────────────┐   ┌──────────────┐   ┌─────────────┐   ┌─────────────┐
│Cola         │──▶│Update Saldo  │──▶│Registro DB  │──▶│RabbitMQ     │
│Background   │   │              │   │             │   │             │
└─────────────┘   └──────────────┘   └─────────────┘   └─────────────┘
```

#### InmediateService (Híbrido)
```
┌─────────────┐   ┌──────────────┐   ┌─────────────┐
│Validaciones │──▶│Update Saldo  │──▶│Respuesta    │
│             │   │Inmediato     │   │+Saldo       │
└─────────────┘   └──────────────┘   └─────────────┘
                         │
                         ▼
                  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐
                  │Cola         │──▶│Registro DB  │──▶│RabbitMQ     │
                  │Background   │   │             │   │             │
                  └─────────────┘   └─────────────┘   └─────────────┘
```

### Casos de Uso Recomendados

| **Escenario** | **Servicio Recomendado** | **Razón** |
|---------------|---------------------------|-----------|
| **API Bancaria Tradicional** | SolicitudService | Requiere ID inmediato para auditoria |
| **API Mobile/Web Alta Concurrencia** | BackgroundService | Respuesta ultra rápida |
| **Dashboard en Tiempo Real** | InmediateService | Saldo actualizado inmediato |
| **Batch Processing** | SolicitudService | Control total del flujo |
| **Microservicios** | BackgroundService | Desacoplamiento máximo |

### Métricas de Performance (Estimadas)

| **Métrica** | **SolicitudService** | **BackgroundService** | **InmediateService** |
|-------------|---------------------|----------------------|---------------------|
| **Latencia P95** | 500-800ms | 50-100ms | 200-300ms |
| **Throughput** | 100-200 req/s | 1000+ req/s | 500-800 req/s |
| **CPU Usage** | Alto | Medio | Medio-Alto |
| **Memory Usage** | Bajo | Medio | Medio |
| **DB Connections** | Alta concurrencia | Baja inicial | Media |

### Configuración por Ambiente

#### Desarrollo
```csharp
// Configuración conservadora para debugging
private readonly int cantidadReintentos = 5;
private readonly int tiempoMinimoEsperaMs = 100;
private readonly int tiempoMaximoEsperaMs = 200;
```

#### Testing
```csharp
// Configuración para detectar race conditions
private readonly int cantidadReintentos = 15;
private readonly int tiempoMinimoEsperaMs = 10;
private readonly int tiempoMaximoEsperaMs = 50;
```

#### Producción
```csharp
// Configuración optimizada para performance
private readonly int cantidadReintentos = 10;
private readonly int tiempoMinimoEsperaMs = 25;
private readonly int tiempoMaximoEsperaMs = 75;
```

### Patrones de Error por Servicio

#### Errores Comunes por Servicio

| **Error** | **SolicitudService** | **BackgroundService** | **InmediateService** |
|-----------|---------------------|----------------------|---------------------|
| **Status 97** | Deadlocks en SaveChanges | Deadlocks en background | Deadlocks en saldo inmediato |
| **Status 98** | RowVersion conflicts | Background RowVersion | Saldo + Background conflicts |
| **Status 99** | DB connection issues | Queue processing errors | Mixed transaction errors |

#### Monitoreo Específico

```csharp
// SolicitudService - Monitorear tiempo total
_logger.LogInformation("SolicitudService: {TiempoTotal}ms para cuenta {Cuenta}", 
    stopwatch.ElapsedMilliseconds, dto.NumeroCuenta);

// BackgroundService - Monitorear cola depth
_logger.LogInformation("BackgroundService: Cola {Particion} depth: {Depth}", 
    particion, _colas[particion].Reader.Count);

// InmediateService - Monitorear split timing
_logger.LogInformation("InmediateService: Saldo {TiempoSaldo}ms, Queue {TiempoQueue}ms", 
    tiempoSaldo, tiempoQueue);
```

### Migración Entre Servicios

#### De SolicitudService a BackgroundService
```csharp
// Cambio en Controller
// Antes:
var resultado = _solicitudService.RegistrarSolicitudConSaldo(dto);

// Después:
var resultado = _backgroundQueueService.EncolarSolicitud(dto);
// Nota: resultado.Id será 0, manejar en cliente
```

#### De BackgroundService a InmediateService
```csharp
// Sin cambios en Controller, solo configuración DI
services.AddScoped<ISolicitudCommandQueueService, SolicitudCommandQueueInmediateService>();
```

### Debugging y Troubleshooting

#### Logs Clave para Diagnóstico

1. **Inicio de Transacción**
   ```csharp
   _logger.LogDebug("Iniciando transacción serializable para cuenta {Cuenta}", dto.NumeroCuenta);
   ```

2. **Conflictos de Concurrencia**
   ```csharp
   _logger.LogWarning("RowVersion conflict en cuenta {Cuenta}, reintento {Reintento}", 
       dto.NumeroCuenta, reintentos);
   ```

3. **Deadlocks**
   ```csharp
   _logger.LogWarning("Deadlock detectado en cuenta {Cuenta}, esperando {Tiempo}ms", 
       dto.NumeroCuenta, tiempoEspera);
   ```

#### Queries para Diagnóstico

```sql
-- Verificar contención por cuenta
SELECT 
    Numero,
    COUNT(*) as MovimientosSimultaneos
FROM Cuentas c
JOIN SolicitudesDebito s ON c.Id = s.CuentaId
WHERE s.FechaReal > DATEADD(minute, -5, GETDATE())
GROUP BY Numero
ORDER BY MovimientosSimultaneos DESC;

-- Verificar distribución de colas
SELECT 
    (Numero % 10) + 1 as ColaCalculada,
    COUNT(*) as Cantidad
FROM Cuentas
GROUP BY (Numero % 10) + 1
ORDER BY ColaCalculada;
```

### Checklist de Implementación

#### Para SolicitudService
- [ ] Transacción serializable completa
- [ ] Manejo de todos los códigos de error
- [ ] Logging detallado de performance
- [ ] Validación de ID generado
- [ ] RabbitMQ resiliente

#### Para BackgroundService  
- [ ] Validaciones rápidas sin bloqueos
- [ ] Cola configurada correctamente
- [ ] Background processing robusto
- [ ] Monitoreo de profundidad de cola
- [ ] Graceful shutdown

#### Para InmediateService
- [ ] Transacción para saldo inmediato
- [ ] Cola para procesamiento diferido
- [ ] Manejo de errores mixtos
- [ ] Sincronización entre fases
- [ ] Rollback de saldo en errores

---

*Matriz mantida por: Sistema de Handler de Autorización*  
*Última actualización: 9 de octubre de 2025*