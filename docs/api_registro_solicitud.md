# Documentación del Endpoint de Registro de Solicitud

## Endpoint

`POST /api/solicitud`

Registra una solicitud de movimiento sobre una cuenta. Aplica la lógica de negocio, controla idempotencia, tipo de movimiento y saldo suficiente, y publica el resultado en la cola correspondiente de RabbitMQ.

---

## DTO de Entrada: `RegistroSolicitudDto`

| Campo               | Tipo    | Descripción                                                        |
|---------------------|---------|--------------------------------------------------------------------|
| NumeroCuenta        | long    | Número de cuenta sobre la que se realiza el movimiento              |
| Monto               | decimal | Monto del movimiento                                               |
| TipoMovimiento      | string  | Tipo de movimiento: `debito`, `credito`, `contrasiento_debito`, `contrasiento_credito` |
| MovimientoOriginalId| int?    | (Opcional) Id de movimiento original para contrasientos             |
| NumeroComprobante   | long    | Número de comprobante único para idempotencia                      |

---

## DTO de Salida: `SolicitudResultadoDto`

| Campo | Tipo    | Descripción                                      |
|-------|---------|--------------------------------------------------|
| Id    | int     | Id de la solicitud registrada (0 si no autorizada)|
| Saldo | decimal | Saldo final de la cuenta                         |
| Status| int     | Código de estado de la operación                 |

### Tabla de valores de `Status`

| Código | Descripción                                                                 |
|--------|-----------------------------------------------------------------------------|
| 0      | Solicitud autorizada correctamente                                          |
| 1      | La cuenta no existe                                                         |
| 2      | La solicitud ya existe (idempotencia)                                       |
| 3      | Tipo de movimiento no válido                                                |
| 4      | Saldo insuficiente para el movimiento                                       |

---

## Ejemplo de uso

### Solicitud válida
```json
POST /api/solicitud
{
  "NumeroCuenta": 1000000001,
  "Monto": 150.00,
  "TipoMovimiento": "debito",
  "NumeroComprobante": 1234567890
}
```
Respuesta:
```json
{
  "id": 101,
  "saldo": 850.00,
  "status": 0
}
```

### Solicitud con saldo insuficiente
```json
POST /api/solicitud
{
  "NumeroCuenta": 1000000001,
  "Monto": 99999.00,
  "TipoMovimiento": "debito",
  "NumeroComprobante": 1234567891
}
```
Respuesta:
```json
{
  "id": 0,
  "saldo": 850.00,
  "status": 4
}
```

### Solicitud con tipo de movimiento inválido
```json
POST /api/solicitud
{
  "NumeroCuenta": 1000000001,
  "Monto": 100.00,
  "TipoMovimiento": "transferencia",
  "NumeroComprobante": 1234567892
}
```
Respuesta:
```json
{
  "id": 0,
  "saldo": 850.00,
  "status": 3
}
```

### Solicitud duplicada (idempotencia)
```json
POST /api/solicitud
{
  "NumeroCuenta": 1000000001,
  "Monto": 150.00,
  "TipoMovimiento": "debito",
  "NumeroComprobante": 1234567890
}
```
Respuesta:
```json
{
  "id": 0,
  "saldo": 850.00,
  "status": 2
}
```

### Solicitud sobre cuenta inexistente
```json
POST /api/solicitud
{
  "NumeroCuenta": 9999999999,
  "Monto": 100.00,
  "TipoMovimiento": "debito",
  "NumeroComprobante": 1234567893
}
```
Respuesta:
```json
{
  "id": 0,
  "saldo": 0.00,
  "status": 1
}
```
