# Script para Limpiar Colas RabbitMQ (cola_11 a cola_18)
# Este script elimina las colas que ya no se utilizan después de implementar el límite de 10 colas

param(
    [string]$RabbitHost = "172.16.57.184",
    [int]$RabbitPort = 15672,  # Puerto de Management API
    [string]$RabbitUser = "prueba",
    [string]$RabbitPassword = "Censys2300*",
    [string]$VirtualHost = "/",
    [switch]$DryRun = $false  # Solo mostrar qué se haría sin ejecutar
)

Write-Host "🐰 Script de Limpieza de Colas RabbitMQ" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""

# Configurar credenciales para la API de RabbitMQ
$base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($RabbitUser):$($RabbitPassword)"))
$headers = @{
    Authorization = "Basic $base64AuthInfo"
    'Content-Type' = 'application/json'
}

# URL base de la API de Management
$baseUrl = "http://$($RabbitHost):$($RabbitPort)/api"

# Función para hacer peticiones a la API de RabbitMQ
function Invoke-RabbitMQAPI {
    param(
        [string]$Endpoint,
        [string]$Method = "GET",
        [hashtable]$Body = @{}
    )
    
    try {
        $url = "$baseUrl$Endpoint"
        if ($Method -eq "GET") {
            return Invoke-RestMethod -Uri $url -Headers $headers -Method $Method
        } else {
            return Invoke-RestMethod -Uri $url -Headers $headers -Method $Method -Body ($Body | ConvertTo-Json)
        }
    }
    catch {
        Write-Warning "Error en petición a $url`: $($_.Exception.Message)"
        return $null
    }
}

# Función para obtener información de una cola
function Get-QueueInfo {
    param([string]$QueueName)
    
    $vhostEncoded = [System.Web.HttpUtility]::UrlEncode($VirtualHost)
    $queueEncoded = [System.Web.HttpUtility]::UrlEncode($QueueName)
    return Invoke-RabbitMQAPI -Endpoint "/queues/$vhostEncoded/$queueEncoded"
}

# Función para eliminar una cola
function Remove-Queue {
    param([string]$QueueName)
    
    $vhostEncoded = [System.Web.HttpUtility]::UrlEncode($VirtualHost)
    $queueEncoded = [System.Web.HttpUtility]::UrlEncode($QueueName)
    return Invoke-RabbitMQAPI -Endpoint "/queues/$vhostEncoded/$queueEncoded" -Method "DELETE"
}

# Función para purgar mensajes de una cola (vaciarla sin eliminarla)
function Clear-Queue {
    param([string]$QueueName)
    
    $vhostEncoded = [System.Web.HttpUtility]::UrlEncode($VirtualHost)
    $queueEncoded = [System.Web.HttpUtility]::UrlEncode($QueueName)
    return Invoke-RabbitMQAPI -Endpoint "/queues/$vhostEncoded/$queueEncoded/contents" -Method "DELETE"
}

# Verificar conectividad con RabbitMQ
Write-Host "🔍 Verificando conectividad con RabbitMQ..." -ForegroundColor Yellow
$overview = Invoke-RabbitMQAPI -Endpoint "/overview"
if ($null -eq $overview) {
    Write-Error "❌ No se puede conectar a RabbitMQ Management API en $RabbitHost`:$RabbitPort"
    Write-Host "💡 Verifica que:"
    Write-Host "   - RabbitMQ Management Plugin esté habilitado"
    Write-Host "   - Las credenciales sean correctas"
    Write-Host "   - El puerto $RabbitPort esté abierto"
    exit 1
}

Write-Host "✅ Conectado exitosamente a RabbitMQ" -ForegroundColor Green
Write-Host "   Versión: $($overview.rabbitmq_version)" -ForegroundColor Gray
Write-Host "   Nodo: $($overview.node)" -ForegroundColor Gray
Write-Host ""

# Colas a procesar (de la 11 a la 18)
$colasAProcesar = @()
for ($i = 11; $i -le 18; $i++) {
    $colasAProcesar += "cola_$i"
}

Write-Host "🎯 Colas a procesar: $($colasAProcesar -join ', ')" -ForegroundColor Cyan
Write-Host ""

# Procesar cada cola
$colasEncontradas = @()
$colasEliminadas = @()
$colasNoEncontradas = @()

foreach ($cola in $colasAProcesar) {
    Write-Host "🔍 Procesando cola: $cola" -ForegroundColor White
    
    # Verificar si la cola existe
    $queueInfo = Get-QueueInfo -QueueName $cola
    
    if ($null -eq $queueInfo) {
        Write-Host "   ℹ️  Cola '$cola' no existe" -ForegroundColor Gray
        $colasNoEncontradas += $cola
        continue
    }
    
    $colasEncontradas += $cola
    $messageCount = $queueInfo.messages
    $consumerCount = $queueInfo.consumers
    
    Write-Host "   📊 Mensajes: $messageCount, Consumidores: $consumerCount" -ForegroundColor Gray
    
    if ($DryRun) {
        Write-Host "   🔄 [DRY RUN] Se eliminaría la cola '$cola'" -ForegroundColor Yellow
    } else {
        # Primero purgar mensajes si los hay
        if ($messageCount -gt 0) {
            Write-Host "   🧹 Purgando $messageCount mensajes..." -ForegroundColor Yellow
            $purgeResult = Clear-Queue -QueueName $cola
            if ($null -ne $purgeResult) {
                Write-Host "   ✅ Mensajes purgados" -ForegroundColor Green
            } else {
                Write-Warning "   ⚠️  Error al purgar mensajes"
            }
        }
        
        # Eliminar la cola
        Write-Host "   🗑️  Eliminando cola..." -ForegroundColor Yellow
        $deleteResult = Remove-Queue -QueueName $cola
        if ($null -eq $deleteResult) {
            Write-Host "   ✅ Cola '$cola' eliminada exitosamente" -ForegroundColor Green
            $colasEliminadas += $cola
        } else {
            Write-Warning "   ❌ Error al eliminar cola '$cola'"
        }
    }
    
    Write-Host ""
}

# Resumen final
Write-Host "📋 RESUMEN DE OPERACIONES" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan
Write-Host "🔍 Colas procesadas: $($colasAProcesar.Count)" -ForegroundColor White
Write-Host "✅ Colas encontradas: $($colasEncontradas.Count)" -ForegroundColor Green
Write-Host "❌ Colas no encontradas: $($colasNoEncontradas.Count)" -ForegroundColor Gray

if ($DryRun) {
    Write-Host "🔄 Modo DRY RUN - Ninguna cola fue modificada" -ForegroundColor Yellow
} else {
    Write-Host "🗑️  Colas eliminadas: $($colasEliminadas.Count)" -ForegroundColor Green
}

if ($colasEncontradas.Count -gt 0) {
    Write-Host ""
    Write-Host "📝 Colas encontradas:" -ForegroundColor White
    foreach ($cola in $colasEncontradas) {
        $status = if ($DryRun) { "PENDIENTE" } else { if ($cola -in $colasEliminadas) { "ELIMINADA" } else { "ERROR" } }
        $color = if ($status -eq "ELIMINADA") { "Green" } elseif ($status -eq "PENDIENTE") { "Yellow" } else { "Red" }
        Write-Host "   - $cola`: $status" -ForegroundColor $color
    }
}

if ($colasNoEncontradas.Count -gt 0) {
    Write-Host ""
    Write-Host "📝 Colas no encontradas:" -ForegroundColor Gray
    foreach ($cola in $colasNoEncontradas) {
        Write-Host "   - $cola" -ForegroundColor Gray
    }
}

Write-Host ""
if ($DryRun) {
    Write-Host "💡 Para ejecutar realmente, ejecuta el script sin el parámetro -DryRun" -ForegroundColor Yellow
} else {
    Write-Host "🎉 Operación completada" -ForegroundColor Green
}

# Mostrar comandos útiles
Write-Host ""
Write-Host "🔧 COMANDOS ÚTILES:" -ForegroundColor Cyan
Write-Host "==================" -ForegroundColor Cyan
Write-Host "Ejecutar en modo prueba (sin cambios):"
Write-Host "   .\limpiar_colas_rabbit.ps1 -DryRun" -ForegroundColor Gray
Write-Host ""
Write-Host "Ejecutar con parámetros personalizados:"
Write-Host "   .\limpiar_colas_rabbit.ps1 -RabbitHost '172.16.57.184' -RabbitUser 'prueba' -RabbitPassword 'Censys2300*'" -ForegroundColor Gray
Write-Host ""
Write-Host "Verificar Management Web UI:"
Write-Host "   http://$RabbitHost`:$RabbitPort" -ForegroundColor Gray