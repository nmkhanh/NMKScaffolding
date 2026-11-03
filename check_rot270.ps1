$res = Invoke-RestMethod -Uri "http://127.0.0.1:4848/audit_instances" -Method Post -Body "{}" -ContentType "application/json"
$rot270 = $res.data.instances | Where-Object { $_.rotDeg -eq 270 }
Write-Host "Elements with rotDeg = 270: $($rot270.Count)"
$rot270 | Select-Object id, family, type, x, y, z, rotDeg | Format-Table
