@echo off
REM Lee la cantidad de colas desde el archivo de configuración usando PowerShell
for /f %%C in ('powershell -Command "(Get-Content Handler/Config/RabbitConfig.json | ConvertFrom-Json).CantidadColas"') do set COLAS=%%C

REM Compila solo una vez antes de lanzar los workers
dotnet build Worker/Worker.csproj

REM Lanza un worker por cada cola
set FIRST=1
for /L %%i in (1,1,%COLAS%) do (
    if !FIRST! == 1 (
        set FIRST=0
        start cmd /k dotnet run --project Worker/Worker.csproj -- --environment Development --config appsettings.cola_%%i.json
    ) else (
        start cmd /k dotnet run --no-build --project Worker/Worker.csproj -- --environment Development --config appsettings.cola_%%i.json
    )
)