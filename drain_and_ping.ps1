$baseUrl = "http://127.0.0.1:4848"
for ($i = 0; $i -lt 5; $i++) {
    try {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        # Call a lightweight task that raises ExternalEvent, e.g. status or flex_test with empty
        # Wait, status does not raise ExternalEvent!
        # An action that raises ExternalEvent is any path other than /ping and /status!
        # e.g. /audit_instances with a short timeout
        $res = Invoke-RestMethod -Uri "$baseUrl/audit_instances" -Method Post -Body "{}" -ContentType "application/json" -TimeoutSec 15
        Write-Host "Success on attempt $i in $($sw.ElapsedMilliseconds) ms! Total: $($res.data.totalInstances)"
        break
    } catch {
        Write-Host "Attempt $i failed: $($_.Exception.Message)"
    }
    Start-Sleep -Seconds 1
}
