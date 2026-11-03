$baseUrl = "http://127.0.0.1:4848"
$sw = [System.Diagnostics.Stopwatch]::StartNew()
try {
    Write-Host "Sending status_check to Revit..."
    $res = Invoke-RestMethod -Uri "$baseUrl/execute" -Method Post -Body '{"action":"ping"}' -ContentType "application/json" -TimeoutSec 20
    Write-Host "SUCCESS in $($sw.ElapsedMilliseconds) ms: $($res.data.activeDoc)"
} catch {
    Write-Host "FAILED in $($sw.ElapsedMilliseconds) ms: $($_.Exception.Message)"
}
