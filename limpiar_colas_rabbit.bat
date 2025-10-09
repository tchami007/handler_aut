@echo off
echo 🐰 Script de Limpieza de Colas RabbitMQ (cola_11 a cola_18)
echo =========================================================
echo.

REM Configuración de RabbitMQ
set RABBIT_HOST=172.16.57.184
set RABBIT_PORT=15672
set RABBIT_USER=prueba
set RABBIT_PASSWORD=Censys2300*
set VHOST=/

REM Codificar credenciales para Basic Auth
echo %RABBIT_USER%:%RABBIT_PASSWORD% > temp_auth.txt
certutil -encode temp_auth.txt temp_auth_b64.txt > nul
for /f "skip=1 delims=" %%x in (temp_auth_b64.txt) do set "AUTH_B64=%%x" & goto :continue
:continue
del temp_auth.txt temp_auth_b64.txt

echo 🔍 Verificando conectividad con RabbitMQ...
curl -s -u %RABBIT_USER%:%RABBIT_PASSWORD% "http://%RABBIT_HOST%:%RABBIT_PORT%/api/overview" > nul
if errorlevel 1 (
    echo ❌ Error: No se puede conectar a RabbitMQ Management API
    echo 💡 Verifica que:
    echo    - RabbitMQ Management Plugin esté habilitado
    echo    - Las credenciales sean correctas
    echo    - El puerto %RABBIT_PORT% esté abierto
    pause
    exit /b 1
)

echo ✅ Conectado exitosamente a RabbitMQ
echo.

echo 🎯 Procesando colas de la 11 a la 18...
echo.

REM Procesar cada cola
for /L %%i in (11,1,18) do (
    echo 🔍 Procesando cola_%%i...
    
    REM Verificar si la cola existe
    curl -s -u %RABBIT_USER%:%RABBIT_PASSWORD% "http://%RABBIT_HOST%:%RABBIT_PORT%/api/queues/%%2F/cola_%%i" -o temp_queue_info.json
    
    REM Verificar si el archivo contiene información válida (no error 404)
    findstr /C:"\"name\"" temp_queue_info.json > nul
    if errorlevel 1 (
        echo    ℹ️  Cola 'cola_%%i' no existe
    ) else (
        REM Obtener información de mensajes
        for /f "tokens=2 delims=:" %%m in ('findstr /C:"\"messages\"" temp_queue_info.json') do (
            set "MESSAGE_COUNT=%%m"
            set "MESSAGE_COUNT=!MESSAGE_COUNT:,=!"
            set "MESSAGE_COUNT=!MESSAGE_COUNT: =!"
        )
        
        echo    📊 Cola encontrada con mensajes
        
        REM Purgar mensajes si los hay
        echo    🧹 Purgando mensajes...
        curl -s -X DELETE -u %RABBIT_USER%:%RABBIT_PASSWORD% "http://%RABBIT_HOST%:%RABBIT_PORT%/api/queues/%%2F/cola_%%i/contents" > nul
        
        REM Eliminar la cola
        echo    🗑️  Eliminando cola...
        curl -s -X DELETE -u %RABBIT_USER%:%RABBIT_PASSWORD% "http://%RABBIT_HOST%:%RABBIT_PORT%/api/queues/%%2F/cola_%%i" > nul
        
        if errorlevel 1 (
            echo    ❌ Error al eliminar cola_%%i
        ) else (
            echo    ✅ Cola 'cola_%%i' eliminada exitosamente
        )
    )
    
    del temp_queue_info.json 2>nul
    echo.
)

echo 📋 OPERACIÓN COMPLETADA
echo ======================
echo.
echo 🎉 Se procesaron las colas de la 11 a la 18
echo 💡 Las colas que existían fueron purgadas y eliminadas
echo.
echo 🔧 Para verificar el estado actual, visita:
echo    http://%RABBIT_HOST%:%RABBIT_PORT%
echo.
pause