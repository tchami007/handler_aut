@echo off
REM Script de precalentamiento para el Handler usando comprobante único
set HANDLER_URL=http://172.16.57.235:5000
set ENDPOINT_SOLICITUD=/api/solicitud
set NUMERO_CUENTA=1000000001
set MONTO=1000
set DELAY_MS=500
set REPETICIONES=10

timeout /t 5

for /L %%i in (1,1,%REPETICIONES%) do (
    REM Débito con comprobante único
    powershell -Command "Invoke-RestMethod -Uri '%HANDLER_URL%%ENDPOINT_SOLICITUD%' -Method Post -Body (@{numeroCuenta=%NUMERO_CUENTA%; monto=%MONTO%; tipoMovimiento='debito'; numeroComprobante=100000+%%i} | ConvertTo-Json) -ContentType 'application/json'"
    powershell -Command "Start-Sleep -Milliseconds %DELAY_MS%"
    REM Crédito con comprobante único
    powershell -Command "Invoke-RestMethod -Uri '%HANDLER_URL%%ENDPOINT_SOLICITUD%' -Method Post -Body (@{numeroCuenta=%NUMERO_CUENTA%; monto=%MONTO%; tipoMovimiento='credito'; numeroComprobante=200000+%%i} | ConvertTo-Json) -ContentType 'application/json'"
    powershell -Command "Start-Sleep -Milliseconds %DELAY_MS%"
)

echo Precalentamiento finalizado.
