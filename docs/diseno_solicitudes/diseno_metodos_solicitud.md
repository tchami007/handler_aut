# Diseño de Métodos de Solicitud - Handler de Autorización

## 📋 Resumen General

Este documento detalla la arquitectura y decisiones de diseño para los métodos de procesamiento de solicitudes de movimiento en el Handler de Autorización.

**Fecha:** 9 de octubre de 2025  
**Versión:** 3.0 (Unificada)  
**Estado:** Implementado y Unificado

---

## 🏗️ Arquitectura de Servicios

### Servicios Implementados

1. **`SolicitudService`** - Servicio principal tradicional
2. **`SolicitudCommandQueueBackgroundService`** - Procesamiento diferido
3. **`SolicitudCommandQueueInmediateService`** - Procesamiento inmediato con cola

---

## 🔄 Patrones de Procesamiento

### 1. SolicitudService (Tradicional)
```
Cliente → Validaciones → Actualización Saldo → Registro DB → RabbitMQ → Respuesta
```
- **Flujo:** Secuencial completo
- **Respuesta:** Con ID de solicitud generado
- **Uso:** APIs tradicionales que requieren ID inmediato

### 2. SolicitudCommandQueueBackgroundService (Diferido)
```
Cliente → Validaciones Rápidas → Respuesta Inmediata
                ↓
        Cola Background → Actualización Saldo → Registro DB → RabbitMQ
```
- **Flujo:** Validación inmediata + procesamiento diferido
- **Respuesta:** Sin ID (se procesa después)
- **Uso:** APIs de alta performance

### 3. SolicitudCommandQueueInmediateService (Híbrido)
```
Cliente → Validaciones → Actualización Saldo Inmediata → Respuesta
                                    ↓
                        Cola Background → Registro DB → RabbitMQ
```
- **Flujo:** Saldo inmediato + registro diferido
- **Respuesta:** Con saldo actualizado real
- **Uso:** Balance entre performance y consistencia

---

## 🔒 Configuración de Transacciones

### Nivel de Aislamiento: SERIALIZABLE

**Decisión:** Usar `IsolationLevel.Serializable` en operaciones críticas de saldo.

```csharp
using (var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
{
    // Operaciones críticas
    transaction.Commit();
}
```

**Razones:**
- ✅ **Máxima consistencia** en operaciones concurrentes
- ✅ **Previene phantom reads** y dirty reads
- ✅ **Compatible con RowVersion** para control optimista
- ⚠️ **Trade-off:** Mayor latencia por bloqueos

### Estrategia de Entity Framework

**Configuración:**
```csharp
// ❌ NO usar EnableRetryOnFailure (incompatible con transacciones manuales)
services.AddDbContext<HandlerDbContext>(options =>
    options.UseSqlServer(connectionString));
```

**Razón:** Las transacciones manuales con reintentos son incompatibles con la estrategia automática de EF Core.

---

## 🔄 Sistema de Reintentos

### Configuración Unificada

```csharp
private readonly int cantidadReintentos = 10;
private readonly int tiempoMinimoEsperaMs = 50;
private readonly int tiempoMaximoEsperaMs = 100;
```

### Patrón de Implementación

```csharp
int reintentos = cantidadReintentos;
while (reintentos-- > 0)
{
    using (var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
    {
        try
        {
            // Lógica de negocio
            transaction.Commit();
            return resultado;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            transaction.Rollback();
            if (reintentos == 0) return new SolicitudResultadoDto { Status = 98 };
            Thread.Sleep(new Random().Next(50, 100));
            continue;
        }
        catch (Exception ex) when (ex.Message.Contains("deadlock"))
        {
            transaction.Rollback();
            if (reintentos == 0) return new SolicitudResultadoDto { Status = 97 };
            Thread.Sleep(new Random().Next(50, 100));
            continue;
        }
    }
}
```

### Decisiones de Diseño

1. **Tiempos Aleatorios (50-100ms):** Evita sincronización de reintentos entre hilos
2. **10 Reintentos:** Balance entre resiliencia y latencia
3. **Rollback Explícito:** Garantiza consistencia en errores

---

## 📊 Códigos de Estado Unificados

| **Código** | **Significado** | **Descripción** | **Acción Sugerida** |
|------------|-----------------|----------------|-------------------|
| **0** | ✅ **Autorizada** | Operación exitosa | Continuar |
| **1** | ❌ **Cuenta no encontrada** | Número de cuenta inválido | Verificar número |
| **2** | ❌ **Idempotencia** | Ya existe solicitud autorizada hoy | Verificar duplicado |
| **3** | ❌ **Tipo movimiento inválido** | Tipo no permitido | Corregir tipo |
| **4** | ❌ **Saldo insuficiente** | No hay fondos suficientes | Verificar saldo |
| **5** | ❌ **Handler inactivo** | Sistema en mantenimiento | Reintentar después |
| **97** | ❌ **Error por bloqueo** | Deadlocks/timeouts persistentes | Reintentar operación |
| **98** | ❌ **Error concurrencia** | Conflictos de concurrencia | Reintentar con delay |
| **99** | ❌ **Error crítico** | Errores no recuperables | Revisar logs |

---

## 🚨 Manejo de Excepciones

### Jerarquía de Captura

1. **`DbUpdateConcurrencyException`** → Status 98
   - Control de concurrencia optimista con RowVersion
   - Reintenta automáticamente

2. **Deadlocks/Timeouts/Locks** → Status 97
   - `ex.Message.Contains("deadlock")`
   - `ex.Message.Contains("timeout")`
   - `ex.Message.Contains("lock")`
   - Reintenta con delay aleatorio

3. **Excepciones Generales** → Status 99
   - Errores no recuperables
   - Se logea y termina

### Logging Estratégico

```csharp
// Warning para reintentos
_logger.LogWarning(ex, "Conflicto detectado para cuenta {NumeroCuenta}. Reintentando... ({Reintentos} restantes)", 
    dto.NumeroCuenta, reintentos);

// Error para fallos finales
_logger.LogError(ex, "No se pudo resolver el conflicto para cuenta {NumeroCuenta} después de {CantidadReintentos} reintentos", 
    dto.NumeroCuenta, cantidadReintentos);
```

---

## 🗃️ Control de Concurrencia

### Estrategia: Concurrencia Optimista + Transacciones Serializables

#### Tabla Cuentas
```csharp
public class Cuenta
{
    public int Id { get; set; }
    public long Numero { get; set; }
    public decimal Saldo { get; set; }
    public byte[]? RowVersion { get; set; } // ← Control de concurrencia
}
```

#### Flujo de Control
1. **Lectura:** Se obtiene RowVersion actual
2. **Modificación:** Se actualiza saldo
3. **Escritura:** EF verifica que RowVersion no cambió
4. **Conflicto:** Si cambió, lanza `DbUpdateConcurrencyException`
5. **Reintento:** Se vuelve a intentar con nuevos datos

---

## 🔍 Validaciones de Negocio

### Secuencia de Validaciones (Común a todos los servicios)

1. **Estado del Handler**
   ```csharp
   if (!statusService.EstaActivo()) return Status 5;
   ```

2. **Existencia de Cuenta**
   ```csharp
   if (cuenta == null) return Status 1;
   ```

3. **Control de Idempotencia**
   ```csharp
   var existe = db.SolicitudesDebito.Any(s =>
       s.CuentaId == cuenta.Id &&
       s.Monto == dto.Monto &&
       s.NumeroComprobante == dto.NumeroComprobante &&
       s.FechaSolicitud.Date == DateTime.UtcNow.Date &&
       s.Estado == "autorizada");
   if (existe) return Status 2;
   ```

4. **Validación de Tipo de Movimiento**
   ```csharp
   var tiposValidos = ["debito", "credito", "contrasiento_debito", "contrasiento_credito"];
   if (!tiposValidos.Contains(dto.TipoMovimiento)) return Status 3;
   ```

5. **Validación de Saldo Suficiente**
   ```csharp
   var esDebito = dto.TipoMovimiento == "debito" || dto.TipoMovimiento == "contrasiento_credito";
   if (esDebito && cuenta.Saldo < dto.Monto) return Status 4;
   ```

---

## 🎯 Distribución de Colas

### Algoritmo de Particionamiento

```csharp
private int CalcularCola(long numeroCuenta)
{
    if (_cantidadColas == 0) return 1;
    int resultadoModulo = (int)(numeroCuenta % _cantidadColas);
    return resultadoModulo + 1; // Las colas van de cola_1 a cola_N
}
```

**Objetivo:** Distribuir carga de manera uniforme basada en número de cuenta.

---

## 📈 Métricas y Monitoreo

### Logs Importantes

1. **Conflictos de Concurrencia**
   - Frecuencia de reintentos por cuenta
   - Patrones de cuentas problemáticas

2. **Performance**
   - Tiempo promedio de procesamiento
   - Tasa de éxito/fallo por servicio

3. **Colas**
   - Distribución de mensajes por cola
   - Latencia de procesamiento diferido

### Alertas Sugeridas

- **Status 97/98 > 5% del total:** Revisar concurrencia
- **Status 99 > 1% del total:** Investigar errores críticos
- **Tiempo promedio > 500ms:** Revisar performance
- **Reintentos promedio > 3:** Revisar configuración de base de datos

---

## 🔧 Configuración de Producción

### Parámetros Recomendados

```csharp
// Desarrollo
private readonly int cantidadReintentos = 10;
private readonly int tiempoMinimoEsperaMs = 50;
private readonly int tiempoMaximoEsperaMs = 100;

// Producción (alta concurrencia)
private readonly int cantidadReintentos = 15;
private readonly int tiempoMinimoEsperaMs = 25;
private readonly int tiempoMaximoEsperaMs = 75;
```

### Base de Datos

- **Isolation Level:** SERIALIZABLE para operaciones críticas
- **Connection Pooling:** Configurar según carga esperada
- **Timeouts:** Configurar timeouts adecuados para evitar bloqueos largos

---

## 📚 Referencias y Evolución

### Versiones

- **v1.0:** Implementación básica sin transacciones
- **v2.0:** Transacciones serializables + EnableRetryOnFailure (problemático)
- **v3.0:** Transacciones serializables + reintentos manuales (actual)

### Lecciones Aprendidas

1. **EnableRetryOnFailure de EF Core es incompatible** con transacciones manuales
2. **Tiempos aleatorios son cruciales** para evitar sincronización de reintentos
3. **Isolation Serializable es necesario** para consistencia en alta concurrencia
4. **Códigos de error unificados** simplifican el debugging y monitoreo

### Próximas Mejoras

- [ ] Métricas automáticas con Prometheus
- [ ] Circuit breaker para cuentas problemáticas
- [ ] Cache de validaciones para mejorar performance
- [ ] Batching de operaciones para reducir contención

---

## 🚀 Guía de Implementación Rápida

### Para Nuevos Métodos de Solicitud

1. **Copiar patrón de reintentos** de cualquier servicio actual
2. **Usar transacciones serializables** para operaciones de saldo
3. **Implementar códigos de error unificados** (97, 98, 99)
4. **Agregar logging apropiado** para debugging
5. **Testear bajo carga** para validar concurrencia

### Checklist de Calidad

- [ ] ¿Usa transacciones serializables?
- [ ] ¿Implementa reintentos con tiempos aleatorios?
- [ ] ¿Maneja DbUpdateConcurrencyException?
- [ ] ¿Maneja deadlocks y timeouts?
- [ ] ¿Usa códigos de error unificados?
- [ ] ¿Incluye logging apropiado?
- [ ] ¿Valida estado del handler?
- [ ] ¿Controla idempotencia?

---

*Documento mantido por: Sistema de Handler de Autorización*  
*Última actualización: 9 de octubre de 2025*