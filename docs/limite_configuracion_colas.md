# Límite de Configuración de Colas - Handler de Autorización

## Descripción del Problema

Cada Worker en el sistema requiere un archivo de configuración específico (`appsettings.cola_N.json`) para poder procesar una cola de RabbitMQ. Actualmente solo hay archivos de configuración para 10 colas (cola_1 hasta cola_10), pero el sistema permitía configurar más colas sin límite, causando que Workers adicionales no pudieran iniciar por falta de configuración.

## Solución Implementada

Se implementó un límite máximo de **10 colas** en el sistema para que coincida con el número de archivos de configuración disponibles del Worker.

### Cambios Realizados

#### 1. ConfigColasService.cs
- **Método `AgregarCola()`**: Se agregó validación para no permitir más de 10 colas
- **Método `SetColas()`**: Se agregó validación para no permitir configurar más de 10 colas mediante actualización directa

```csharp
// Límite máximo de colas basado en archivos de configuración del Worker disponibles
const int MAX_COLAS = 10;
if (config.Colas.Count >= MAX_COLAS)
    throw new InvalidOperationException($"No se pueden agregar más colas. Límite máximo: {MAX_COLAS}");
```

#### 2. RabbitConfig.json
- Se redujo la configuración actual de 18 colas a 10 colas
- Se ajustó `CantidadColas` de 18 a 10

#### 3. Tests
- Se agregó test `AgregarCola_LimiteMaximo_Retorna_BadRequest()` para verificar que el límite funciona correctamente
- El test verifica que al intentar agregar la 11va cola se retorne BadRequest con mensaje de error apropiado

### Archivos de Configuración del Worker Disponibles

```
Worker/
├── appsettings.cola_1.json
├── appsettings.cola_2.json
├── appsettings.cola_3.json
├── appsettings.cola_4.json
├── appsettings.cola_5.json
├── appsettings.cola_6.json
├── appsettings.cola_7.json
├── appsettings.cola_8.json
├── appsettings.cola_9.json
└── appsettings.cola_10.json
```

### Comportamiento

1. **Al agregar colas mediante API**: El endpoint `POST /api/config/colas/agregar` retornará BadRequest si se intenta agregar más de 10 colas
2. **Al configurar colas directamente**: El endpoint `POST /api/config/colas` retornará BadRequest si se envía una lista con más de 10 colas
3. **Mensaje de error**: Se incluye el límite máximo en el mensaje de error para facilitar el debugging

### Ampliación del Límite

Para ampliar el límite en el futuro:

1. **Crear archivos de configuración adicionales** en Worker:
   ```
   appsettings.cola_11.json
   appsettings.cola_12.json
   ...
   ```

2. **Actualizar el límite** en `ConfigColasService.cs`:
   ```csharp
   const int MAX_COLAS = 15; // O el número deseado
   ```

3. **Actualizar el test** para verificar el nuevo límite

### API Endpoints Afectados

- `POST /api/config/colas/agregar` - Agrega una nueva cola (con límite)
- `POST /api/config/colas` - Configura colas directamente (con límite)

### Fecha de Implementación
9 de octubre de 2025

### Estado
✅ Implementado y probado