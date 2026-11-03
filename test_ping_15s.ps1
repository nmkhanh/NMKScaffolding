$sw = [System.Diagnostics.Stopwatch]::StartNew()
try {
    Write-Host "Sending request with 15s timeout..."
    $res = Invoke-RestMethod -Uri "http://127.0.0.1:4848/execute" -Method Post -Body '{"action":"ping"}' -ContentType "application/json" -TimeoutSec 15
    Write-Host "SUCCESS in $($sw.ElapsedMilliseconds) ms: $($res.data.activeDoc)"
} catch {
    Write-Host "FAILED in $($sw.ElapsedMilliseconds) ms: $($_.Exception.Message)"
}
