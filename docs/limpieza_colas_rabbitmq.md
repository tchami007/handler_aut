# 🧹 Herramientas de Limpieza de Colas RabbitMQ

Este documento explica cómo limpiar las colas de RabbitMQ de la cola_11 a la cola_18 que ya no se utilizan después de implementar el límite de 10 colas.

## 📁 Archivos Disponibles

### 1. **PowerShell Script** (Recomendado)
- **Archivo**: `limpiar_colas_rabbit.ps1`
- **Ventajas**: Más robusto, mejor manejo de errores, modo dry-run
- **Requiere**: PowerShell 5.0+ y acceso a Internet

### 2. **Batch Script** (CMD)
- **Archivo**: `limpiar_colas_rabbit.bat`
- **Ventajas**: Funciona en CMD tradicional, no requiere PowerShell
- **Requiere**: curl instalado en Windows

### 3. **Herramienta C#** (Más integrada)
- **Archivo**: `Tools/LimpiadorColasRabbitMQ.cs`
- **Ventajas**: Integrada con el proyecto, más control programático
- **Requiere**: Compilación del proyecto Tools

## 🚀 Instrucciones de Uso

### Opción 1: PowerShell Script (Recomendado)

```powershell
# 1. Ejecutar en modo prueba (sin hacer cambios)
.\limpiar_colas_rabbit.ps1 -DryRun

# 2. Ejecutar la limpieza real
.\limpiar_colas_rabbit.ps1

# 3. Usar configuración personalizada
.\limpiar_colas_rabbit.ps1 -RabbitHost "172.16.57.184" -RabbitUser "prueba" -RabbitPassword "Censys2300*"
```

**Requisitos:**
- PowerShell 5.0 o superior
- RabbitMQ Management Plugin habilitado
- Puerto 15672 abierto (Management API)

### Opción 2: Batch Script (CMD)

```cmd
# Ejecutar directamente
limpiar_colas_rabbit.bat
```

**Requisitos:**
- curl instalado en Windows
- RabbitMQ Management Plugin habilitado
- Puerto 15672 abierto (Management API)

### Opción 3: Herramienta C#

```bash
# 1. Compilar el proyecto
cd Tools
dotnet build

# 2. Ejecutar en modo prueba
dotnet run -- --dry-run

# 3. Ejecutar la limpieza real
dotnet run
```

**Requisitos:**
- .NET 9.0
- Puerto 5672 abierto (protocolo AMQP)

## 🔧 Configuración de RabbitMQ

### Management Plugin

Para usar los scripts de PowerShell y Batch, asegúrate de que el Management Plugin esté habilitado:

```bash
# Habilitar Management Plugin
rabbitmq-plugins enable rabbitmq_management

# Verificar que esté funcionando
curl http://172.16.57.184:15672/api/overview
```

### Puertos Requeridos

- **Puerto 5672**: Protocolo AMQP (para herramienta C#)
- **Puerto 15672**: Management API (para scripts PowerShell/Batch)

### Credenciales

Las herramientas usan por defecto:
- **Host**: 172.16.57.184
- **Usuario**: prueba
- **Contraseña**: Censys2300*
- **Virtual Host**: /

## 📊 Qué Hacen las Herramientas

### Colas Procesadas
Se procesan las colas: `cola_11`, `cola_12`, `cola_13`, `cola_14`, `cola_15`, `cola_16`, `cola_17`, `cola_18`

### Proceso de Limpieza
1. **Verificar conectividad** con RabbitMQ
2. **Buscar cada cola** en el rango 11-18
3. **Purgar mensajes** si los hay
4. **Eliminar la cola** completamente
5. **Mostrar resumen** de operaciones

### Modo Dry-Run
- Simula la operación sin hacer cambios
- Muestra qué se haría sin ejecutarlo
- Recomendado ejecutar primero

## 🛡️ Seguridad y Precauciones

### ⚠️ Advertencias Importantes

1. **Backup**: Asegúrate de que no hay datos importantes en las colas 11-18
2. **Modo Dry-Run**: SIEMPRE ejecuta primero en modo prueba
3. **Horario**: Ejecuta durante ventanas de mantenimiento
4. **Verificación**: Confirma que los Workers no estén consumiendo estas colas

### 🔍 Verificaciones Previas

```bash
# Verificar estado de colas antes de limpiar
curl -u prueba:Censys2300* "http://172.16.57.184:15672/api/queues/%2F/cola_11"
curl -u prueba:Censys2300* "http://172.16.57.184:15672/api/queues/%2F/cola_12"
# ... etc
```

### 📋 Checklist Pre-Ejecución

- [ ] RabbitMQ Management está funcionando
- [ ] No hay Workers consumiendo colas 11-18
- [ ] Se ejecutó en modo dry-run exitosamente
- [ ] Se tiene acceso de administrador a RabbitMQ
- [ ] Se verificó que no hay mensajes importantes en las colas

## 🔄 Resultados Esperados

### Salida Exitosa
```
🐰 Script de Limpieza de Colas RabbitMQ
==========================================

🔍 Verificando conectividad con RabbitMQ...
✅ Conectado exitosamente a RabbitMQ

🎯 Procesando colas de la 11 a la 18...

🔍 Procesando cola_11...
   📊 Mensajes: 0, Consumidores: 0
   🗑️  Eliminando cola...
   ✅ Cola 'cola_11' eliminada exitosamente

...

📋 RESUMEN DE OPERACIONES
=========================
🔍 Colas procesadas: 8
✅ Colas encontradas: 8
🗑️  Colas eliminadas: 8
🎉 Operación completada
```

### Si una Cola No Existe
```
🔍 Procesando cola_15...
   ℹ️  Cola 'cola_15' no existe
```

## 🆘 Solución de Problemas

### Error de Conexión
```
❌ Error: No se puede conectar a RabbitMQ Management API
```
**Soluciones:**
1. Verificar que RabbitMQ esté ejecutándose
2. Habilitar Management Plugin: `rabbitmq-plugins enable rabbitmq_management`
3. Verificar credenciales y hostname
4. Comprobar firewall en puerto 15672

### Error de Autenticación
```
❌ Error 401: Unauthorized
```
**Soluciones:**
1. Verificar usuario y contraseña
2. Verificar permisos del usuario en RabbitMQ
3. Verificar Virtual Host

### Cola Con Consumidores Activos
```
⚠️  Advertencia: La cola tiene consumidores activos
```
**Soluciones:**
1. Detener Workers que consuman esas colas
2. Verificar que no hay aplicaciones conectadas
3. Esperar a que se desconecten automáticamente

## 🔗 Enlaces Útiles

- **RabbitMQ Management UI**: http://172.16.57.184:15672
- **API Documentation**: https://rawcdn.githack.com/rabbitmq/rabbitmq-server/v3.12.x/deps/rabbitmq_management/priv/www/api/index.html
- **RabbitMQ Docs**: https://www.rabbitmq.com/docs

## 📝 Logs y Auditoria

Después de ejecutar cualquier herramienta, verifica en RabbitMQ Management UI que las colas fueron eliminadas correctamente en la sección **Queues**.

Las herramientas generan logs detallados que puedes guardar para auditoria:

```powershell
# Guardar logs de PowerShell
.\limpiar_colas_rabbit.ps1 > limpieza_$(Get-Date -Format 'yyyyMMdd_HHmmss').log
```

```cmd
# Guardar logs de Batch
limpiar_colas_rabbit.bat > limpieza_%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%.log
```