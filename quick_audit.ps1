$sw = [System.Diagnostics.Stopwatch]::StartNew()
$res = Invoke-RestMethod -Uri 'http://127.0.0.1:4848/audit_instances' -Method Post -Body '{}' -ContentType 'application/json' -TimeoutSec 10
Write-Host "Total instances: $($res.data.totalInstances) | Elapsed ms: $($sw.ElapsedMilliseconds)"
$branch = $res.data.instances | Where-Object { $_.x -ge 28000 -and $_.y -le -1200 -and $_.z -ge 0 }
Write-Host "Elements on -Y branch: $($branch.Count)"
$branch | Group-Object family | Select-Object Name, Count | Format-Table
