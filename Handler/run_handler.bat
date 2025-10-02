@echo off
REM Inicia la aplicación Handler en modo desarrollo con HTTPS
cd /d %~dp0\..\Handler
call dotnet run --launch-profile https
pause
