$p1 = (Get-Process -Id 15480).CPU
Start-Sleep -Seconds 1
$p2 = (Get-Process -Id 15480).CPU
$delta = $p2 - $p1
Write-Host "Revit CPU delta in 1s: $delta seconds"
