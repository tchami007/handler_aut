# Tareas Detalladas – Etapa 2

## Etapa 2: Implementación de APIs Principales del Handler  
**Duración estimada:** 4 días

### Tareas detalladas y orden de precedencia

1. **Implementar endpoint Health**
   - Crear endpoint para verificación de estado del Handler.
   - Validar respuesta básica (OK, versión, estado).

2. **Implementar endpoint ActivarHandler**
   - Permitir activar el servicio Handler vía API.
   - Registrar estado de activación en base de datos.

3. **Implementar endpoint InactivarHandler**
   - Permitir desactivar el servicio Handler vía API.
   - Registrar estado de inactivación en base de datos.

4. **Implementar endpoint ConfigurarColas**
   - Permitir configuración dinámica de colas RabbitMQ desde el Handler.
   - Validar persistencia y actualización de parámetros.

5. **Implementar endpoint Estadística**
   - Exponer métricas y estadísticas de operación del Handler.
   - Consultar datos agregados de solicitudes, saldos y logs.

6. **Implementar endpoint SaldoCuentaByCuenta**
   - Permitir consulta de saldo del handler por número de cuenta.
   - Validar acceso y respuesta segura.

7. **Implementar endpoint SaldoCuentaAll**
   - Permitir consulta de saldos de todas las cuentas del handler.
   - Validar paginación y seguridad de la respuesta.

8. **Implementar lógica de registro y consulta de saldos**
   - Registrar solicitud de movimiento (debito, credito, contrasiento debito, contrasiento credito) en tabla de solicitudes del handler
   - Actualizar saldos segun tipo de solicitud en tabla de handler
   - Exponer métodos de consulta y validación de saldos.

9. **Configurar persistencia de solicitudes y estados**
   - Registrar cada solicitud y su estado en la base de datos.
   - Validar integridad y consistencia de los datos.

10. **Pruebas de verificación**
    - Ejecutar scripts de test sobre los endpoints implementados.
    - Validar respuestas, persistencia y funcionamiento esperado.

---

Cada tarea debe completarse en el orden indicado para asegurar dependencias y coherencia funcional. Al finalizar, realizar comprobación manual y documentar resultados.
