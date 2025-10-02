# Diseño Técnico – Handler de Autorización de Débitos

## 1. Arquitectura General
- Componente intermediario entre el receptor ISO y el Core bancario.
- Comunicación vía API REST para recepción de solicitudes.
- Procesamiento asíncrono mediante colas dedicadas por cuenta.

## 2. Persistencia y Durabilidad
- Uso de Message Broker (RabbitMQ) con almacenamiento en disco para colas.
- Registro persistente de solicitudes y estados en base de datos (SQL Server).
- Garantía de recuperación automática tras caídas o reinicios.

## 3. Control de Concurrencia
- Cada cuenta tiene una cola dedicada para procesar solicitudes en orden.
- Control estricto para evitar condiciones de carrera y duplicidad de movimientos.
- Las solicitudes de una misma cuenta nunca se mezclan en colas diferentes.

## 4. Evaluación y Autorización
- Consulta de saldo local antes de autorizar provisionalmente.
- Descuento provisional del saldo y registro de la operación.
- Procesamiento posterior con el Core para confirmación definitiva.

## 5. Manejo de Timeouts y Fallback
- Respuesta al receptor en menos de 3 segundos.
- Si el Core no responde, se autoriza provisionalmente y se mantiene estado pendiente.
- Gestión de reversión o regularización si el Core rechaza por saldo insuficiente.

## 6. Escalabilidad y Recuperación ante Fallos
- Diseño orientado a alta concurrencia y escalabilidad horizontal de workers.
- Recuperación automática de colas y solicitudes tras caídas.
- Sin pérdida de información ni inconsistencias en el proceso.

## 7. Trazabilidad y Auditoría
- Registro detallado de cada solicitud, estado y resultado en repositorio persistente.
- Facilita auditoría y análisis de operaciones ante incidentes o reclamos.

---

# Handler de autorización

- Tipo de aplicación
    - Se trata de una aplicación Web API .NET Core  version 8, en esta instancia de prototipo
- Expone sus funciones por APIs
- Comunica eventos por un administrador de colas.
- Persistencia
    - Inicialmente SQL Server para repositorio interno. Como se usa ORM para almacenamiento se podrá modificar a otra base de datos a futuro.
- Integración
    - Entity Framework para almacenamiento interno.
    - ADO.net para disparo de procesos a la base Bansys
    - RabbitMQ para cola de eventos
- Implementación en 3 capas:
    - Presentación → Carpeta controller
    - Lógica de negocios → Carpeta service
    - Infrastructure → Carpeta Repositorio
        - Clases para manejo de cola
        - Clases para manejo de EF
        - Clases para manejo de Sps
- Modelos → Carpeta Model para diseños internos
- Utils → Carpeta Shared para ubicar las clases transversales
- Apis →
    - GET → Health (indica el estado de handler)
      - Iniciando (procesando cola)
      - Activo
      - Inactivo
    - POST → ActivarHandler
    - POST → InactivarHandler
    - POST → ConfigurarColas (establece el parametro de cantidad de colas)
    - GET → Estadística (indicadores de procesamiento, longitud de cola, tiempo promedio respuesta, etc)
    - GET → SaldoCuentaByCuenta (información de cuenta - saldo)
    - GET → SaldoCuentaAll (información de cuenta - saldo)
    - POST → ProcesarSolicitud (procesa el registro de una solicitud de débito)
    - POST → ProcesarCola (recarga la cola de eventos, con registros no procesados del repositorio interno, luego procesa los eventos)
    - PUT → ActualizarSaldo (actualiza el saldo del handler)
- Seguridad → JWT sin refresh token, inicialmente

# Worker de solicitudes

- Tipo de aplicación
    - Se trata de una aplicación híbrida de servicio + web api .NET Core version 8, instanciable por cada cola que maneje HANDLER, en esta instancia de prototipo
- Expone funciones de activación y consulta estado por APIs. 
- Ejecuta su función principal de procesamiento en segundo plano por medio de un Background process con cancelation token
- Persistencia → no posee propia, usa la base de datos de handler para actualización de respuestas (estado + saldos)
- Integración
    - Entity Framework para actualización en base de datos de handler
    - ADO.net para disparo de procesos a la base Bansys
    - Cliente RabbitMQ para acceder a eventos
- Implementación en 3 capas:
    - Presentación → Carpeta controller para apis
    - Lógica de negocios → Codigo worker
    - Infrastructure → Carpeta Repositorio
      - Clases para actualización del estados/saldos de handler
      - Clases para manejo de cola
      - Clases para manejo de Sps
- Modelos → Carpeta Model para diseños internos
- Utils → Carpeta Shared para ubicar las clases transversales
- Apis →
    - GET → Health (indica el estado de worker)
        - Activo
        - Inactivo
    - POST → ActivarWorker 
    - POST → InactivarWorker
- Seguridad → JWT sin refresh token, inicialmente



