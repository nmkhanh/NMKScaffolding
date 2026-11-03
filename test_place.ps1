$b = @{familyName='1'; x=0; y=0; z=0; rotDeg=0} | ConvertTo-Json
$t0 = [System.Diagnostics.Stopwatch]::StartNew()
try {
    $res = Invoke-RestMethod -Uri 'http://127.0.0.1:4848/place_instance' -Method Post -Body $b -ContentType 'application/json' -TimeoutSec 5
    Write-Host "Time: $($t0.ElapsedMilliseconds) ms | Success: $($res.success) | ID: $($res.data.instanceId)"
} catch {
    Write-Host "Error: $($_.Exception.Message)"
}
