$sw = [System.Diagnostics.Stopwatch]::StartNew()
Write-Host "Sending audit_instances request (Timeout 60s)..."
$res = Invoke-RestMethod -Uri "http://127.0.0.1:4848/audit_instances" -Method Post -Body "{}" -ContentType "application/json" -TimeoutSec 60
Write-Host "COMPLETED in $($sw.ElapsedMilliseconds) ms!"
Write-Host "Total instances in document: $($res.data.totalInstances)"
$m2 = $res.data.instances | Where-Object { $_.x -ge 28000 }
Write-Host "Elements in Model 2: $($m2.Count)"
$m2 | Group-Object family | Select-Object Name, Count | Format-Table
Write-Host "--- Family 5 in Model 2 ---"
$m2 | Where-Object { $_.family -eq "5" } | Select-Object id, x, y, z, rotDeg | Format-Table
Write-Host "--- Family 1 in Model 2 ---"
$m2 | Where-Object { $_.family -eq "1" } | Select-Object id, x, y, z, rotDeg | Format-Table
