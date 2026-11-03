$res = Invoke-RestMethod -Uri "http://127.0.0.1:4848/audit_instances" -Method Post -Body "{}" -ContentType "application/json"
$branch = $res.data.instances | Where-Object { $_.x -ge 28000 -and $_.y -le -1200 -and $_.z -ge 0 }
Write-Host "Elements on -Y branch: $($branch.Count)"
$branch | Group-Object family | Select-Object Name, Count | Format-Table
Write-Host "--- Family 5 on -Y branch ---"
$branch | Where-Object { $_.family -eq "5" } | Select-Object id, x, y, z, rotDeg | Format-Table
Write-Host "--- Family 1 on -Y branch ---"
$branch | Where-Object { $_.family -eq "1" } | Select-Object id, x, y, z, rotDeg | Format-Table
