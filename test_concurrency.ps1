# 🧪 Script de Pruebas de Concurrencia - Incremental

## Configuración
$baseUrl = "http://172.16.57.235:5000"
$endpoint = "$baseUrl/api/SolicitudCommand/encolar"

## Datos de prueba
$cuentasPrueba = @(1000000755, 1000000250, 1000001001, 1000001002, 1000001003)
$montos = @(50, 100, 25, 75, 150)

## NIVEL 1: Prueba Básica (5 requests secuenciales)
Write-Host "🔄 NIVEL 1: Prueba Básica (5 requests secuenciales)" -ForegroundColor Yellow

for ($i = 0; $i -lt 5; $i++) {
    $cuenta = $cuentasPrueba[$i % $cuentasPrueba.Length]
    $monto = $montos[$i % $montos.Length]
    
    $body = @{
        numeroCuenta = $cuenta
        monto = $monto
        numeroComprobante = "TEST-$(Get-Date -Format 'yyyyMMdd-HHmmss')-$i"
        tipoMovimiento = "debito"
        movimientoOriginalId = $null
    } | ConvertTo-Json
    
    Write-Host "  📤 Request $($i+1): Cuenta $cuenta, Monto $monto"
    
    try {
        $response = Invoke-RestMethod -Uri $endpoint -Method POST -Body $body -ContentType "application/json"
        Write-Host "  ✅ Response: Status=$($response.status), Saldo=$($response.saldo)" -ForegroundColor Green
    }
    catch {
        Write-Host "  ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Start-Sleep -Seconds 1
}

## NIVEL 2: Prueba de Concurrencia Baja (10 requests paralelos)
Write-Host "`n🔄 NIVEL 2: Prueba de Concurrencia Baja (10 requests paralelos)" -ForegroundColor Yellow

$jobs = @()
for ($i = 0; $i -lt 10; $i++) {
    $cuenta = $cuentasPrueba[$i % $cuentasPrueba.Length]
    $monto = $montos[$i % $montos.Length]
    
    $scriptBlock = {
        param($url, $numeroCuenta, $montoParam, $requestId)
        
        $body = @{
            numeroCuenta = $numeroCuenta
            monto = $montoParam
            numeroComprobante = "CONC-$(Get-Date -Format 'yyyyMMdd-HHmmss')-$requestId"
            tipoMovimiento = "debito"
            movimientoOriginalId = $null
        } | ConvertTo-Json
        
        try {
            $response = Invoke-RestMethod -Uri $url -Method POST -Body $body -ContentType "application/json"
            return @{
                RequestId = $requestId
                Success = $true
                Status = $response.status
                Saldo = $response.saldo
                Cuenta = $numeroCuenta
                Error = $null
            }
        }
        catch {
            return @{
                RequestId = $requestId
                Success = $false
                Status = -1
                Saldo = 0
                Cuenta = $numeroCuenta
                Error = $_.Exception.Message
            }
        }
    }
    
    $job = Start-Job -ScriptBlock $scriptBlock -ArgumentList $endpoint, $cuenta, $monto, $i
    $jobs += $job
    Write-Host "  🚀 Iniciado job $i para cuenta $cuenta"
}

Write-Host "  ⏳ Esperando resultados..."
$results = $jobs | Wait-Job | Receive-Job
$jobs | Remove-Job

$successCount = ($results | Where-Object { $_.Success }).Count
$errorCount = ($results | Where-Object { -not $_.Success }).Count

Write-Host "  📊 Resultados: $successCount exitosos, $errorCount errores" -ForegroundColor Cyan

if ($errorCount -gt 0) {
    Write-Host "  ❌ Errores encontrados:" -ForegroundColor Red
    $results | Where-Object { -not $_.Success } | ForEach-Object {
        Write-Host "    Request $($_.RequestId): $($_.Error)" -ForegroundColor Red
    }
}

## NIVEL 3: Prueba de Concurrencia Media (25 requests paralelos en la misma cuenta)
Write-Host "`n🔄 NIVEL 3: Prueba de Concurrencia Media (25 requests en cuenta $($cuentasPrueba[0]))" -ForegroundColor Yellow

$cuentaTest = $cuentasPrueba[0]
$jobs = @()

for ($i = 0; $i -lt 25; $i++) {
    $monto = 10  # Monto pequeño para evitar saldo insuficiente
    
    $job = Start-Job -ScriptBlock $scriptBlock -ArgumentList $endpoint, $cuentaTest, $monto, $i
    $jobs += $job
}

Write-Host "  ⏳ Esperando resultados de 25 requests concurrentes..."
$results = $jobs | Wait-Job | Receive-Job
$jobs | Remove-Job

$successCount = ($results | Where-Object { $_.Success }).Count
$errorCount = ($results | Where-Object { -not $_.Success }).Count
$deadlockErrors = ($results | Where-Object { $_.Error -like "*deadlock*" }).Count

Write-Host "  📊 Resultados: $successCount exitosos, $errorCount errores, $deadlockErrors deadlocks" -ForegroundColor Cyan

## Análisis de saldos finales
$successfulResults = $results | Where-Object { $_.Success -and $_.Status -eq 0 }
if ($successfulResults.Count -gt 0) {
    $saldoFinal = ($successfulResults | Select-Object -Last 1).Saldo
    $transaccionesExitosas = $successfulResults.Count
    $montoTotalDebited = $transaccionesExitosas * 10
    Write-Host "  💰 Saldo final: $saldoFinal, Transacciones exitosas: $transaccionesExitosas, Monto total: $montoTotalDebited" -ForegroundColor Green
}

if ($errorCount -gt 0) {
    Write-Host "  ❌ Errores por tipo:" -ForegroundColor Red
    $results | Where-Object { -not $_.Success } | Group-Object Error | ForEach-Object {
        Write-Host "    $($_.Name): $($_.Count) occurrencias" -ForegroundColor Red
    }
}

## Resumen
Write-Host "`n📋 RESUMEN DE PRUEBAS" -ForegroundColor Magenta
Write-Host "✅ Nivel 1 (Secuencial): Completado"
Write-Host "✅ Nivel 2 (10 paralelos): $successCount exitosos de 10"
Write-Host "✅ Nivel 3 (25 misma cuenta): $successCount exitosos de 25"

if ($deadlockErrors -eq 0) {
    Write-Host "🎉 ¡Sin errores de deadlock detectados!" -ForegroundColor Green
} else {
    Write-Host "⚠️ $deadlockErrors errores de deadlock detectados" -ForegroundColor Yellow
}

Write-Host "`nPruebas completadas. Revisar logs para detalles adicionales." -ForegroundColor Cyan