# Quick Reference - Códigos de Error y Patrones

## 🚨 Códigos de Error - Referencia Rápida

### Estados Exitosos
| Código | Estado | Descripción |
|--------|--------|-------------|
| **0** | ✅ **AUTORIZADA** | Operación completada exitosamente |

### Estados de Rechazo por Validación (1-5)
| Código | Estado | Descripción | Acción Cliente |
|--------|--------|-------------|----------------|
| **1** | ❌ **CUENTA_NO_ENCONTRADA** | Número de cuenta inválido | Verificar número de cuenta |
| **2** | ❌ **IDEMPOTENCIA** | Ya existe solicitud autorizada hoy | Verificar duplicados |
| **3** | ❌ **TIPO_MOVIMIENTO_INVALIDO** | Tipo no permitido | Usar: debito, credito, contrasiento_debito, contrasiento_credito |
| **4** | ❌ **SALDO_INSUFICIENTE** | No hay fondos suficientes | Verificar saldo disponible |
| **5** | ❌ **HANDLER_INACTIVO** | Sistema en mantenimiento | Reintentar más tarde |

### Estados de Error Técnico (97-99)
| Código | Estado | Descripción | Acción Sistema |
|--------|--------|-------------|----------------|
| **97** | ❌ **ERROR_BLOQUEO** | Deadlocks/timeouts persistentes | Auto-reintenta, alertar si persiste |
| **98** | ❌ **ERROR_CONCURRENCIA** | Conflictos de concurrencia | Auto-reintenta, revisar RowVersion |
| **99** | ❌ **ERROR_CRITICO** | Error no recuperable | Revisar logs inmediatamente |

---

## 🔄 Patrones de Código - Copy & Paste

### Patrón de Reintentos Estándar

```csharp
public SolicitudResultadoDto MetodoEjemplo(RegistroSolicitudDto dto)
{
    using var scope = _serviceProvider.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<HandlerDbContext>();
    var statusService = scope.ServiceProvider.GetRequiredService<IHandlerStatusService>();

    if (!statusService.EstaActivo())
    {
        return new SolicitudResultadoDto { Id = 0, Saldo = 0, Status = 5 };
    }

    int reintentos = cantidadReintentos;
    while (reintentos-- > 0)
    {
        using (var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
        {
            try
            {
                // === VALIDACIONES ESTÁNDAR ===
                var cuenta = db.Cuentas.FirstOrDefault(c => c.Numero == dto.NumeroCuenta);
                if (cuenta == null)
                {
                    transaction.Rollback();
                    return new SolicitudResultadoDto { Id = 0, Saldo = 0, Status = 1 };
                }

                // Control de idempotencia
                var existe = db.SolicitudesDebito.Any(s =>
                    s.CuentaId == cuenta.Id &&
                    s.Monto == dto.Monto &&
                    s.NumeroComprobante == dto.NumeroComprobante &&
                    s.FechaSolicitud.Date == DateTime.UtcNow.Date &&
                    s.Estado == "autorizada");
                    
                if (existe)
                {
                    transaction.Rollback();
                    return new SolicitudResultadoDto { Id = 0, Saldo = cuenta.Saldo, Status = 2 };
                }

                // Validar tipo de movimiento
                var tiposValidos = new[] { "debito", "credito", "contrasiento_debito", "contrasiento_credito" };
                if (!tiposValidos.Contains(dto.TipoMovimiento))
                {
                    transaction.Rollback();
                    return new SolicitudResultadoDto { Id = 0, Saldo = cuenta.Saldo, Status = 3 };
                }

                // Validar saldo suficiente
                var esDebito = dto.TipoMovimiento == "debito" || dto.TipoMovimiento == "contrasiento_credito";
                if (esDebito && cuenta.Saldo < dto.Monto)
                {
                    transaction.Rollback();
                    return new SolicitudResultadoDto { Id = 0, Saldo = cuenta.Saldo, Status = 4 };
                }

                // === LÓGICA DE NEGOCIO ESPECÍFICA ===
                // [Implementar lógica específica aquí]

                transaction.Commit();
                return new SolicitudResultadoDto { Id = 0, Saldo = cuenta.Saldo, Status = 0 };
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
            {
                transaction.Rollback();
                if (reintentos == 0)
                {
                    _logger.LogError(ex, "Conflicto de concurrencia persistente para cuenta {NumeroCuenta}", dto.NumeroCuenta);
                    return new SolicitudResultadoDto { Id = 0, Saldo = 0, Status = 98 };
                }
                _logger.LogWarning(ex, "Conflicto de concurrencia para cuenta {NumeroCuenta}. Reintentando... ({Reintentos} restantes)", dto.NumeroCuenta, reintentos);
                Thread.Sleep(new Random().Next(tiempoMinimoEsperaMs, tiempoMaximoEsperaMs));
                continue;
            }
            catch (Exception ex) when (ex.Message.Contains("deadlock") || ex.Message.Contains("timeout") || ex.Message.Contains("lock"))
            {
                transaction.Rollback();
                if (reintentos == 0)
                {
                    _logger.LogError(ex, "Error de bloqueo persistente para cuenta {NumeroCuenta}", dto.NumeroCuenta);
                    return new SolicitudResultadoDto { Id = 0, Saldo = 0, Status = 97 };
                }
                _logger.LogWarning(ex, "Error de bloqueo para cuenta {NumeroCuenta}. Reintentando... ({Reintentos} restantes)", dto.NumeroCuenta, reintentos);
                Thread.Sleep(new Random().Next(tiempoMinimoEsperaMs, tiempoMaximoEsperaMs));
                continue;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error crítico para cuenta {NumeroCuenta}", dto.NumeroCuenta);
                return new SolicitudResultadoDto { Id = 0, Saldo = 0, Status = 99 };
            }
        }
    }

    // Si llega aquí, se agotaron todos los reintentos
    _logger.LogError("Se agotaron todos los reintentos para cuenta {NumeroCuenta}", dto.NumeroCuenta);
    return new SolicitudResultadoDto { Id = 0, Saldo = 0, Status = 99 };
}
```

### Configuración de Variables Estándar

```csharp
// En la clase del servicio
private readonly int cantidadReintentos = 10;
private readonly int tiempoMinimoEsperaMs = 50;
private readonly int tiempoMaximoEsperaMs = 100;
```

### Patrón de Actualización de Saldo

```csharp
// Dentro de la transacción, después de validaciones
switch (dto.TipoMovimiento)
{
    case "debito":
    case "contrasiento_credito":
        cuenta.Saldo -= dto.Monto;
        break;
    case "credito":
    case "contrasiento_debito":
        cuenta.Saldo += dto.Monto;
        break;
}

db.Cuentas.Update(cuenta);
db.SaveChanges();
```

### Patrón de Registro de Solicitud

```csharp
var solicitud = new SolicitudDebito
{
    CuentaId = cuenta.Id,
    FechaSolicitud = DateTime.UtcNow.Date,
    FechaReal = DateTime.UtcNow,
    TipoMovimiento = dto.TipoMovimiento,
    MovimientoOriginalId = dto.MovimientoOriginalId,
    NumeroComprobante = dto.NumeroComprobante,
    Monto = dto.Monto,
    Estado = "autorizada", // o "rechazada" según validaciones
    SaldoRespuesta = cuenta.Saldo,
    CodigoEstado = status
};

db.SolicitudesDebito.Add(solicitud);
db.SaveChanges();
```

### Patrón de RabbitMQ

```csharp
// Calcular cola destino
int colaDestino = CalcularCola(dto.NumeroCuenta);
string nombreCola = $"cola_{colaDestino}";

// Crear mensaje
var mensajeDto = new SolicitudRabbitDto
{
    Id = solicitud.Id,
    TipoMovimiento = dto.TipoMovimiento,
    Importe = dto.Monto,
    NumeroCuenta = dto.NumeroCuenta,
    FechaMovimiento = DateTime.UtcNow,
    NumeroComprobante = dto.NumeroComprobante,
    Contrasiento = null,
    ConnectionStringBanksys = "<CONNECTION_STRING>"
};

// Publicar
var mensaje = System.Text.Json.JsonSerializer.Serialize(mensajeDto);
try
{
    _publisher.Publish(mensaje, nombreCola);
}
catch (Exception ex)
{
    _logger.LogError(ex, "No se pudo publicar en RabbitMQ para cuenta {NumeroCuenta}", dto.NumeroCuenta);
    // Continuar sin fallar la transacción - RabbitMQ es opcional
}
```

---

## 🔧 Troubleshooting Quick Fix

### Status 97 (Deadlocks) - Alto Volumen
```csharp
// Aumentar tiempo de espera
private readonly int tiempoMinimoEsperaMs = 25;  // era 50
private readonly int tiempoMaximoEsperaMs = 75;  // era 100
private readonly int cantidadReintentos = 15;    // era 10
```

### Status 98 (Concurrencia) - Frecuente
```sql
-- Verificar RowVersion en base de datos
SELECT TOP 10 Numero, RowVersion 
FROM Cuentas 
ORDER BY NEWID();

-- Verificar operaciones simultáneas
SELECT 
    Numero,
    COUNT(*) as OperacionesSimultaneas
FROM Cuentas c
JOIN SolicitudesDebito s ON c.Id = s.CuentaId
WHERE s.FechaReal > DATEADD(minute, -1, GETDATE())
GROUP BY Numero
HAVING COUNT(*) > 1;
```

### Status 99 (Críticos) - Investigación
```csharp
// Agregar logging detallado temporalmente
_logger.LogInformation("Iniciando procesamiento cuenta {Cuenta}, monto {Monto}, tipo {Tipo}", 
    dto.NumeroCuenta, dto.Monto, dto.TipoMovimiento);

_logger.LogInformation("Estado cuenta antes: saldo={Saldo}, rowversion={RowVersion}", 
    cuenta.Saldo, Convert.ToBase64String(cuenta.RowVersion ?? new byte[0]));
```

---

## 📋 Checklist de Implementación Rápida

### Nuevo Método de Solicitud
- [ ] Copiar patrón de reintentos completo
- [ ] Ajustar validaciones específicas en el try principal
- [ ] Implementar lógica de negocio específica
- [ ] Usar transacciones serializables
- [ ] Implementar los 3 catches estándar
- [ ] Configurar logging apropiado
- [ ] Testear con carga concurrente

### Debugging de Problemas
- [ ] Verificar códigos de error en logs
- [ ] Revisar frecuencia de reintentos
- [ ] Validar RowVersion en base de datos
- [ ] Verificar configuración de transacciones
- [ ] Revisar distribución de carga por cuenta
- [ ] Monitorear latencia de base de datos

### Optimización de Performance
- [ ] Ajustar parámetros de reintento según ambiente
- [ ] Verificar distribución de colas
- [ ] Optimizar queries de validación
- [ ] Configurar connection pooling
- [ ] Implementar métricas de monitoreo

---

*Quick Reference mantido por: Sistema de Handler de Autorización*  
*Última actualización: 9 de octubre de 2025*