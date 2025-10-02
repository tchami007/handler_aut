# Tareas Detalladas – Etapa 3

## Etapa 3: Implementación de Procesamiento de Solicitudes y Colas  
**Duración estimada:** 5 días

### Tareas detalladas

1. **Diseño de lógica de encolado por cuenta**
   - Definir estructura de colas por cuenta en RabbitMQ.
   - Implementar asignación dinámica de colas según número de cuenta.
   - Validar que cada cuenta procese sus solicitudes en orden.

2. **Control de concurrencia en el Handler**
   - Implementar mecanismos para evitar procesamiento simultáneo de la misma cuenta.
   - Validar bloqueo/desbloqueo de recursos por cuenta.

3. **Desarrollo del Worker para procesamiento asíncrono**
   - Crear Worker que consuma mensajes de las colas por cuenta.
   - Implementar procesamiento de solicitudes y comunicación con Core.
   - Registrar logs de procesamiento y resultados.

4. **Implementación de timeout y fallback (autorización provisional)**
   - Definir tiempo máximo de espera para respuesta del Core.
   - Implementar lógica de autorización provisional ante timeout.
   - Registrar estado provisional y actualizar al recibir respuesta definitiva.

5. **Integración avanzada con RabbitMQ**
   - Configurar eventos para recuperación ante fallos.
   - Implementar reintentos y manejo de mensajes no procesados.
   - Validar persistencia y consistencia de los eventos.

6. **Pruebas de concurrencia y recuperación**
   - Ejecutar pruebas simulando múltiples solicitudes concurrentes.
   - Validar procesamiento ordenado y correcto por cuenta.
   - Probar escenarios de fallo y recuperación automática.

---

Cada tarea puede ser asignada y monitoreada individualmente para asegurar el avance y la calidad en el procesamiento de solicitudes y colas.
