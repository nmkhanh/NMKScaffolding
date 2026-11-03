$res = Invoke-RestMethod -Uri 'http://127.0.0.1:4848/audit_instances' -TimeoutSec 5
Write-Host "Total instances: $($res.data.Count)"
$res.data | Select-Object -First 30 | Format-Table -Property elementId, familyName, x, y, z, rotDeg, lengthMm, heightMm
