$res = Invoke-RestMethod -Uri "http://127.0.0.1:4848/audit_instances" -Method Post -Body "{}" -ContentType "application/json"
$elems = $res.data.instances | Where-Object { $_.x -ge 14000 }
Write-Host "Total elements at X >= 14000: $($elems.Count)"
$grouped = $elems | Group-Object -Property x, y, z, family, rotDeg | Where-Object { $_.Count -gt 1 }
Write-Host "Duplicate groups count: $($grouped.Count)"
foreach ($g in $grouped) {
    Write-Host "Group key: $($g.Name), Count: $($g.Count)"
    foreach ($item in $g.Group) {
        Write-Host "   ID: $($item.id), Family: $($item.family), Type: $($item.type)"
    }
}
