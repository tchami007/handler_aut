# Pruebas Etapa 2 – Handler de Autorización

Este documento describe el set de pruebas manuales para validar la funcionalidad de los endpoints y lógica principal del Handler, según los objetivos de la etapa 2. El estado de cada prueba debe ser completado por el ejecutor (OK/ERROR/OBSERVACIÓN).

---

## 1. Prueba de Endpoint Health
**Objetivo:** Verificar que el endpoint `/api/status/health` responde correctamente con estado, versión y fecha.
- Solicitud: `GET /api/status/health`
- Esperado: Respuesta 200 con campos `estado`, `version`, `fecha`.
- Estado: Ok, funciona

## 2. Prueba de Activación del Handler
**Objetivo:** Validar que el Handler puede activarse vía API y refleja el estado activo.
- Solicitud: `POST /api/status/activar` (requiere JWT válido)
- Esperado: Respuesta 200 con `{ estado: "activo" }`.
- Estado: 

## 3. Prueba de Inactivación del Handler
**Objetivo:** Validar que el Handler puede inactivarse vía API y refleja el estado inactivo.
- Solicitud: `POST /api/status/inactivar` (requiere JWT válido)
- Esperado: Respuesta 200 con `{ estado: "inactivo" }`.
- Estado: 

## 4. Prueba de Configuración de Colas
**Objetivo:** Verificar la consulta y actualización dinámica de colas RabbitMQ.
- Solicitud: `GET /api/config/colas` y `POST /api/config/colas` (requiere JWT válido)
- Esperado: Consulta devuelve configuración actual; actualización modifica nombres/cantidad de colas.
- Estado: 

## 5. Prueba de Endpoint Estadística
**Objetivo:** Validar que el endpoint `/api/estadistica` expone métricas agregadas y permite filtrar por tipo de movimiento.
- Solicitud: `GET /api/estadistica` y `GET /api/estadistica?tipoMovimiento=debito` (requiere JWT válido)
- Esperado: Respuesta 200 con métricas, saldos, logs y colas.
- Estado: 

## 6. Prueba de Consulta de Saldo por Cuenta
**Objetivo:** Verificar la consulta de saldo por número de cuenta.
- Solicitud: `GET /api/saldo/cuenta/{numeroCuenta}` (requiere JWT válido)
- Esperado: Respuesta 200 con saldo si existe, 404 si no existe.
- Estado: 

## 7. Prueba de Consulta de Saldos de Todas las Cuentas
**Objetivo:** Validar la consulta de saldos de todas las cuentas y paginación.
- Solicitud: `GET /api/saldo/todas` y `GET /api/saldo/todas_paginado?page=1&pageSize=50` (requiere JWT válido)
- Esperado: Respuesta 200 con lista de saldos y paginación correcta.
- Estado: 

## 8. Prueba de Registro de Solicitud de Movimiento
**Objetivo:** Validar el registro de solicitudes de débito, crédito y contrasientos, incluyendo control de idempotencia y saldo suficiente.
- Solicitud: `POST /api/solicitud` (requiere JWT válido)
- Datos: Pruebas con tipos `debito`, `credito`, `contrasiento_debito`, `contrasiento_credito`, comprobante único, cuenta existente/no existente, saldo suficiente/insuficiente.
- Esperado: Respuesta con estado correcto (`autorizada`, `rechazada`), saldo actualizado, publicación en cola.
- Estado: 

## 9. Prueba de Seguridad y Acceso
**Objetivo:** Validar que los endpoints sensibles requieren autenticación JWT y solo usuarios válidos pueden acceder.
- Solicitud: Acceso a endpoints protegidos con y sin JWT, usando usuarios hardcodeados (`admin`, `usuario_1`, `usuario_2`, `test`).
- Esperado: Acceso permitido solo con JWT válido y usuario autorizado.
- Estado: 

## 10. Prueba de Persistencia y Consistencia
**Objetivo:** Verificar que las solicitudes y estados se registran correctamente en la base de datos y que los saldos reflejan los movimientos realizados.
- Solicitud: Consultas y registros de movimientos, verificación en base de datos.
- Esperado: Datos consistentes, sin duplicidad ni pérdida de información.
- Estado: 

---

**Instrucciones:**
- Ejecuta cada prueba manualmente usando Swagger, Postman o curl.
- Completa el campo "Estado" con OK/ERROR/OBSERVACIÓN según el resultado.
- Documenta cualquier hallazgo relevante o bug detectado.

---

**Fin del documento**
