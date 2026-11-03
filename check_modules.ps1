$proc = Get-Process -Name Revit -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "Revit PID: $($proc.Id)"
    foreach ($m in $proc.Modules) {
        if ($m.ModuleName -like '*Scaffolding*' -or $m.ModuleName -like '*Mcp*') {
            Write-Host "Loaded: $($m.FileName)"
        }
    }
} else {
    Write-Host "Revit not running"
}
