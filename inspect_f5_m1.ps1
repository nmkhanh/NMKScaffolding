$res = Invoke-RestMethod -Uri "http://127.0.0.1:4848/audit_instances" -Method Post -Body "{}" -ContentType "application/json"
$f5_m1 = $res.data.instances | Where-Object { $_.family -eq "5" -and $_.x -lt 10000 -and $_.z -gt 0 }
$f5_m1 | Select-Object id, x, y, z, rotDeg, lengthMm | Format-Table
