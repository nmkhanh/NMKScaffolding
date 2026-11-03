$sw = [System.Diagnostics.Stopwatch]::StartNew()
try {
    # Test setting a parameter on an existing element, or placing an element
    Write-Host "Sending set_parameter request..."
    $res = Invoke-RestMethod -Uri "http://127.0.0.1:4848/set_parameter" -Method Post -Body '{"elementId":13133,"paramName":"l","value":1800}' -ContentType "application/json" -TimeoutSec 10
    Write-Host "SUCCESS in $($sw.ElapsedMilliseconds) ms!"
    $res | ConvertTo-Json
} catch {
    Write-Host "FAILED in $($sw.ElapsedMilliseconds) ms: $($_.Exception.Message)"
}
