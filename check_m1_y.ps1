$res = Invoke-RestMethod -Uri "http://127.0.0.1:4848/audit_instances" -Method Post -Body "{}" -ContentType "application/json"
$branch_m1 = $res.data.instances | Where-Object { $_.y -gt 0 -and $_.x -lt 10000 }
Write-Host "Elements in Model 1 +Y branch: $($branch_m1.Count)"
$branch_m1 | Group-Object family | Select-Object Name, Count | Format-Table
Write-Host "--- Family 5 in Model 1 ---"
$res.data.instances | Where-Object { $_.family -eq "5" -and $_.x -lt 10000 } | Select-Object id, x, y, z, rotDeg, lengthMm | Format-Table
