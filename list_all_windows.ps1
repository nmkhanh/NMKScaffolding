Get-Process | Where-Object { $_.MainWindowTitle } | ForEach-Object {
    Write-Host "$($_.ProcessName) (PID $($_.Id)): $($_.MainWindowTitle)"
}
