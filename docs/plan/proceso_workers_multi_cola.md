# Proceso para lanzar múltiples workers RabbitMQ (uno por cola)

## 1. Configuración de colas

- Por cada cola, crea un archivo de configuración en `Worker/`:
  - `appsettings.cola_1.json`
  - `appsettings.cola_2.json`
  - `appsettings.cola_3.json`
  - `appsettings.cola_4.json`
- Cada archivo debe tener la propiedad `Queue` con el nombre de la cola y los datos correctos de conexión:

```json
"RabbitMQ": {
  "Host": "172.16.57.184",
  "Port": 5672,
  "UserName": "prueba",
  "Password": "Censys2300*",
  "VirtualHost": "/",
  "Exchange": "handler_exchange",
  "Queue": "cola_1"
}
```

## 2. Script de lanzamiento automático

El archivo `run_all_workers.bat` ahora detecta automáticamente cuántos workers lanzar leyendo el parámetro `CantidadColas` del archivo `Handler/Config/RabbitConfig.json`.

Ejemplo de script:

```bat
@echo off
REM Lee la cantidad de colas desde el archivo de configuración usando PowerShell
for /f %%C in ('powershell -Command "(Get-Content Handler/Config/RabbitConfig.json | ConvertFrom-Json).CantidadColas"') do set COLAS=%%C

REM Lanza un worker por cada cola
for /L %%i in (1,1,%COLAS%) do (
  start cmd /k dotnet run --project Worker/Worker.csproj -- --environment Development --config appsettings.cola_%%i.json
)
```

De esta forma, solo es necesario actualizar el parámetro `CantidadColas` en la configuración para ajustar el número de workers lanzados.

## 3. Identificación de workers

- Cada consola mostrará el nombre de la cola que está procesando:
  - `[WORKER] Iniciando worker para la cola: cola_1`
  - `[WORKER] Iniciando worker para la cola: cola_2`
  - ...
- Los mensajes recibidos se loguean con el nombre de la cola:
  - `[WORKER:cola_1] Mensaje recibido: ...`

## 4. Recomendaciones

- Si agregas nuevas colas, crea el archivo de configuración y agrega la línea correspondiente en el `.bat`.
- Verifica que la IP, usuario y contraseña sean correctos para tu entorno RabbitMQ.
- Puedes monitorear los workers por consola y por los logs generados.

## 5. Solución de problemas

- Si un worker no conecta, revisa la IP, puerto, usuario y contraseña.
- Si no ves la consola, asegúrate de ejecutar el `.bat` desde el explorador de Windows.
- Para finalizar workers ocultos, usa el Administrador de tareas y termina procesos `dotnet.exe` o `Worker.exe`.

---

**Este proceso permite escalar y aislar el consumo de colas RabbitMQ de forma sencilla y robusta.**
