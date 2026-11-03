$baseUrl = "http://127.0.0.1:4848"
Write-Host "Flushing queue..."
for ($i = 1; $i -le 15; $i++) {
    try {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $res = Invoke-RestMethod -Uri "$baseUrl/execute" -Method Post -Body '{"action":"ping"}' -ContentType "application/json" -TimeoutSec 1
        Write-Host "QUEUE DRAINED! Success on try $i in $($sw.ElapsedMilliseconds) ms: $($res.data.activeDoc)"
        break
    } catch {
        Write-Host "Flush step $i : $($_.Exception.Message)"
    }
    Start-Sleep -Milliseconds 100
}
