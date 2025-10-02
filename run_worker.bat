@echo off
REM Script para ejecutar el Worker en modo desarrollo
cd /d %~dp0\Worker

dotnet run --project Worker.csproj
