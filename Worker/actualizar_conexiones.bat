@echo off
setlocal enabledelayedexpansion

set "connectionString=,\n  \"ConnectionStrings\": {\n    \"BanksysConnection\": \"Server=172.16.57.198;Database=BANKSYS;User Id=sa;Password=Censys2300*;TrustServerCertificate=True;MultipleActiveResultSets=true\"\n  }"

for %%f in (appsettings.cola_*.json) do (
    echo Procesando %%f
    
    rem Comprueba si el archivo ya tiene la configuración ConnectionStrings
    findstr /C:"ConnectionStrings" "%%f" > nul
    if !errorlevel! neq 0 (
        rem No tiene la configuración, así que la agregamos
        type "%%f" | find /v "}" > "%%f.tmp"
        echo !connectionString!>> "%%f.tmp"
        echo }>> "%%f.tmp"
        move /y "%%f.tmp" "%%f"
        echo Actualizado %%f
    ) else (
        echo %%f ya tiene la configuración
    )
)

echo Proceso completado