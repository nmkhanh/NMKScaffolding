$res = Invoke-RestMethod -Uri "http://127.0.0.1:4848/audit_instances" -Method Post -Body "{}" -ContentType "application/json"
$newbies = $res.data.instances | Where-Object { $_.id -ge 14045 }
$newbies | Select-Object id, family, type, x, y, z, rotDeg, lengthMm | Format-Table
