$res = Invoke-RestMethod -Uri "http://127.0.0.1:4848/audit_instances" -Method Post -Body "{}" -ContentType "application/json"
$f5 = $res.data.instances | Where-Object { $_.family -eq "5" -and $_.x -ge 28000 }
Write-Host "Family 5 at X >= 28000: $($f5.Count)"
$f5 | Select-Object id, x, y, z, rotDeg | Format-Table
$f1 = $res.data.instances | Where-Object { $_.family -eq "1" -and $_.x -ge 28000 -and $_.z -ge 0 }
Write-Host "Family 1 at X >= 28000 (active): $($f1.Count)"
$f1 | Select-Object id, x, y, z, rotDeg | Format-Table
