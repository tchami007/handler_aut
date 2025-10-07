# Servicio de Estadística (`EstadisticaService`)

## Descripción General
El `EstadisticaService` es un servicio de la capa de negocio encargado de recopilar y exponer información estadística relevante sobre el estado del sistema de autorización. Su objetivo es brindar una visión consolidada de la operación, integrando datos de la base de datos y del sistema de colas RabbitMQ.

## Funcionalidad Principal
El método principal es:

```csharp
Task<EstadisticaDto> GetEstadisticasAsync(string? tipoMovimiento = null)
```

Este método permite obtener un resumen estadístico general, o filtrado por tipo de movimiento (por ejemplo, "debito" o "credito").

## Datos que expone
El DTO de salida (`EstadisticaDto`) incluye:

- **SolicitudesProcesadas**: Total de solicitudes de débito procesadas (puede filtrar por tipo de movimiento).
- **SolicitudesPorEstado**: Diccionario con el conteo de solicitudes agrupadas por estado (ej: "pendiente", "procesada", "rechazada").
- **SaldoTotalCuentas**: Suma total de los saldos de todas las cuentas.
- **MovimientosPorTipo**: Diccionario con el conteo de solicitudes agrupadas por tipo de movimiento.
- **Errores**: Cantidad total de logs de tipo "error" registrados en la tabla `LogsOperacion`.
- **ColasRabbit**: Lista con el estado de cada cola RabbitMQ configurada:
  - `Nombre`: Nombre de la cola.
  - `MensajesPendientes`: Cantidad de mensajes pendientes en la cola.
  - `Consumidores`: Cantidad de consumidores conectados a la cola.
- **LogsRecientes**: Lista de los 10 logs más recientes (de cualquier tipo) registrados en la tabla `LogsOperacion`.

## Implementación
- **Base de datos**: Utiliza Entity Framework Core para consultar las entidades `SolicitudesDebito`, `Cuentas` y `LogsOperacion`.
- **RabbitMQ**: Lee la configuración de colas desde el archivo `Handler/Config/RabbitConfig.json` y consulta el estado real de cada cola usando la conexión RabbitMQ.
- **DTOs auxiliares**: Utiliza `ColaRabbitDto` y `LogDto` para estructurar la información de colas y logs.

## Consideraciones
- Si no existen registros en la tabla `LogsOperacion`, el array `LogsRecientes` será vacío.
- Si no hay colas configuradas o RabbitMQ no está disponible, el array `ColasRabbit` será vacío o mostrará valores -1.
- El archivo de configuración de colas debe estar en la ruta real del proyecto (`Handler/Config/RabbitConfig.json`).

## Ejemplo de respuesta
```json
{
  "solicitudesProcesadas": 11145,
  "solicitudesPorEstado": {
    "autorizada": 10,
    "actualizada": 11170
  },
  "saldoTotalCuentas": 84932865.92,
  "movimientosPorTipo": {
    "credito": 11187,
    "debito": 11345
  },
  "errores": 0,
  "colasRabbit": [],
  "logsRecientes": []
}
```

## Extensión
Para que el sistema registre logs en la tabla `LogsOperacion`, es necesario implementar explícitamente la escritura de logs en los puntos deseados del sistema.
