$revit2022 = "C:\Program Files\Autodesk\Revit 2022\Revit.exe"

# 1. Try launching through Explorer COM (out-of-process in explorer.exe to bypass Job Object limit)
try {
    $shellWindows = (New-Object -ComObject Shell.Application).Windows()
    $explorerApp = $null
    for ($i = 0; $i -lt $shellWindows.Count; $i++) {
        $item = $shellWindows.Item($i)
        if ($item -and $item.Document -and $item.Document.Application) {
            $explorerApp = $item.Document.Application
            break
        }
    }

    if ($explorerApp) {
        $explorerApp.ShellExecute($revit2022, "", "", "open", 1)
        Write-Host "Dispatched via Explorer Window Application successfully."
    } else {
        # Fallback to direct Shell.Application
        $sh = New-Object -ComObject Shell.Application
        $sh.ShellExecute($revit2022, "", "", "open", 1)
        Write-Host "Dispatched via Shell.Application COM."
    }
} catch {
    Write-Host "COM failed: $($_.Exception.Message). Falling back to Start-Process..."
    Start-Process -FilePath $revit2022
}

Start-Sleep -Seconds 2
Get-Process Revit | Select-Object Id, Path, MainWindowTitle
