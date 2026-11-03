$procs = Get-Process -Name *revit* -ErrorAction SilentlyContinue
foreach ($p in $procs) {
    Write-Host "PID: $($p.Id) | Name: $($p.ProcessName) | Title: '$($p.MainWindowTitle)'"
}
