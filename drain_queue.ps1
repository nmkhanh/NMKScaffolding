$baseUrl = "http://127.0.0.1:4848"

Write-Host "=== DRAINING QUEUE VIA /execute WITH action=ping ==="
for ($i = 1; $i -le 10; $i++) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $body = @{action="ping"; seq=$i} | ConvertTo-Json
        $res = Invoke-RestMethod -Uri "$baseUrl/execute" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 3
        Write-Host "Ping $i succeeded in $($sw.ElapsedMilliseconds) ms: $($res.data.activeDoc)"
        break
    } catch {
        Write-Host "Ping $i failed or timed out: $($_.Exception.Message)"
    }
}
