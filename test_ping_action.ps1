try {
    $b = @{action='ping'} | ConvertTo-Json
    $res = Invoke-RestMethod -Uri "http://127.0.0.1:4848/execute" -Method Post -Body $b -ContentType "application/json" -TimeoutSec 2
    Write-Host "STATUS: FREE! ActiveDoc: $($res.data.activeDoc)"
} catch {
    Write-Host "STATUS: BLOCKED ($($_.Exception.Message))"
}
