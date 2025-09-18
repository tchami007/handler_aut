# Handler de Autorización de Débitos

## Objetivo
Desarrollar un **componente de control y autorización** (Handler Autorización) que gestione solicitudes de débito provenientes de un receptor ISO, coordinando la actualización de saldos con el Core bancario y asegurando la consistencia entre el saldo local y el saldo centralizado.

El componente debe:
- Garantizar respuesta en menos de **3 segundos** al receptor.
- Prevenir inconsistencias entre el saldo local y el Core, aplicando control estricto de concurrencia por cuenta.
- Soportar **alta concurrencia**, **persistencia de operaciones** y **recuperación automática ante fallos**.

---

## Flujo de Autorización

### 1. Recepción de solicitud
- El **Handler** recibe la solicitud vía API REST, con los datos de la cuenta y el monto a debitar.
- Se determina la **cola de procesamiento** correspondiente a la cuenta. Todas las solicitudes de una misma cuenta se asignan siempre a la misma cola (control de concurrencia), aunque una cola puede contener solicitudes de varias cuentas distintas.

### 2. Evaluación y registro
- Se consulta el saldo local (repositorio SQL Server).
- **Si el saldo es suficiente:**
  1. Registrar la solicitud en un **repositorio persistente** (garantizando durabilidad y trazabilidad).
  2. Descontar provisionalmente el saldo local (`saldo = saldo - monto`).
  3. Encolar la solicitud para procesamiento posterior con el Core.
- **Si el saldo es insuficiente:**
  - Responder con rechazo inmediato al receptor.

### 3. Procesamiento asíncrono
- Cada cuenta tiene asociada una **cola dedicada**.
- Un **worker** procesa en orden las solicitudes de esa cuenta, enviándolas al Core y registrando el estado de confirmación o rechazo.

### 4. Timeouts y fallback
- Si el Core no responde en menos de 3 segundos:
  - El **Handler** autoriza provisionalmente el débito (respuesta positiva al receptor).
  - El worker mantiene el estado pendiente de confirmación.
  - Si el Core rechaza por falta de saldo (por competencia con otro canal), el movimiento queda registrado como pendiente y se gestiona la reversión o regularización.

---

## Consideraciones Técnicas

- **Persistencia y recuperación**
  - Uso de un **Message Broker** (RabbitMQ) con almacenamiento en disco para persistencia de colas y recuperación automática tras caídas.

- **Consistencia y concurrencia**
  - Control estricto de concurrencia por cuenta: todas las solicitudes de una cuenta se procesan en la misma cola, garantizando orden y evitando condiciones de carrera.
  - Una cola puede contener solicitudes de varias cuentas, pero nunca se mezclan las solicitudes de una misma cuenta en colas diferentes.

- **Escalabilidad y tolerancia a fallos**
  - El diseño soporta alta concurrencia y permite escalar horizontalmente los workers.
  - El sistema debe ser capaz de recuperarse automáticamente ante caídas, sin pérdida de información ni inconsistencias.
  - Múltiples workers paralelos, cada uno atendiendo un conjunto de colas (sharding por cuenta).  
  - Balanceo horizontal con múltiples instancias del Handler.

- **Resiliencia**  
  - Reintentos configurables al Core en caso de errores.  
  - La conciliacion de saldos, se realiza en forma automatica. Al retorno de la aplicacion en CORE se devuelve el saldo efectivo, el cual pisa el saldo del handler.

---

## Ejemplo de Pseudocódigo

```csharp
// Recepción de solicitud
public async Task<Response> HandleDebitRequest(DebitRequest req) {
    var saldo = SaldoRepository.Get(req.CuentaId);

    if (saldo < req.Monto) {
        return Response.Rechazado("Fondos insuficientes");
    }

    SolicitudRepository.Save(req);
    SaldoRepository.Update(req.CuentaId, saldo - req.Monto);

    ColaManager.Enqueue(req.CuentaId, req);

    return Response.Aceptado("Débito en proceso");
}

// Worker de procesamiento
public async Task ProcesarCola(string cuentaId) {
    while (true) {
        var req = ColaManager.Dequeue(cuentaId);
        var result = CoreApi.EnviarSolicitud(req);

        if (!result.Ok) {
            Logger.Warn("Fallo en core, reintentando...");
            ColaManager.Enqueue(cuentaId, req);
        } else {
            SolicitudRepository.Confirmar(req.Id, result);
        }
    }
}

```

---

## Definiciones Operativas para el Prototipo

### Gestión de errores y reintentos
- Si el Core no responde tras 3 reintentos, la solicitud queda en estado "pendiente" y se reintentará en el próximo ciclo del worker.
- No se implementa estado "expirado"; los pendientes se mantienen hasta que el Core responda o se limpie manualmente.

### Reconciliación y resolución de inconsistencias
- El saldo local se actualiza automáticamente con el saldo efectivo retornado por el Core.
- No se notifican ni registran reconciliaciones forzadas, salvo en el log de la base de datos.

### Auditoría y trazabilidad
- Se registra en el log de la base de datos cada actualización de saldo y cada caída/reinicio de worker.
- No se implementa log externo ni auditoría avanzada.

### Recuperación automática de workers
- Si un worker se cae o deja de funcionar, el sistema debe contar con un mecanismo automático que detecte la caída y lance un nuevo worker que reemplace al anterior. Este nuevo worker debe tomar la cola que estaba siendo procesada por el worker caído, garantizando la continuidad y el procesamiento ordenado de las solicitudes pendientes. Todos los eventos de caída y recuperación deben registrarse en el log de la base de datos para auditoría y monitoreo.

- **Una cola puede contener solicitudes de varias cuentas diferentes.**
  Esto permite que el procesamiento por cuenta sea ordenado y consistente, mientras se aprovecha la concurrencia y el balanceo entre workers.

### Política de locking y concurrencia
- Locking a nivel de registro (cuenta) en base de datos.
- Los deadlocks y locks prolongados se resuelven por la base de datos y se registran en el log.

### Estados adicionales
- Solo se utilizan los estados "pendiente", "autorizada" y "rechazada".
- No se implementan estados "expirado" o "fallido" en el prototipo.

### Pruebas y monitoreo
- Se monitorea el log de la base de datos para detectar caídas de workers, deadlocks y reintentos excesivos.
- No se implementa monitoreo externo ni métricas avanzadas.