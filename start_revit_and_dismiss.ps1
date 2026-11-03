Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Write-Host "Launching Revit 2022..."
$proc = Start-Process "C:\Program Files\Autodesk\Revit 2022\Revit.exe" -PassThru
Write-Host "Revit PID: $($proc.Id)"

$online = $false
for ($i = 0; $i -lt 45; $i++) {
    Start-Sleep -Seconds 1

    try {
        $condition = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ClassNameProperty, "#32770"
        )
        $dialogs = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
            [System.Windows.Automation.TreeScope]::Children, $condition
        )

        foreach ($dlg in $dialogs) {
            $name = $dlg.Current.Name
            if ($name -like "*Security*" -or $name -like "*Add-In*" -or $name -like "*Publisher*") {
                Write-Host "Found Dialog: $name. Clicking Always Load..."
                $btnCond = New-Object System.Windows.Automation.PropertyCondition(
                    [System.Windows.Automation.AutomationElement]::AutomationIdProperty, "CommandLink_1001"
                )
                $btn = $dlg.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
                if (-not $btn) {
                    $btnCond = New-Object System.Windows.Automation.PropertyCondition(
                        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, "CommandLink_1002"
                    )
                    $btn = $dlg.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
                }

                if ($btn) {
                    Write-Host "Clicking button $($btn.Current.AutomationId)..."
                    $invoke = $btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
                    $invoke.Invoke()
                    Write-Host "CLICKED!"
                }
            }
        }
    } catch {}

    try {
        $res = Invoke-RestMethod -Uri "http://127.0.0.1:4848/status" -TimeoutSec 1
        if ($res.status -eq "online") {
            Write-Host "REVIT 2022 MCP BRIDGE IS ONLINE!"
            $online = $true
            break
        }
    } catch {}
}

if (-not $online) {
    Write-Host "Bridge did not come online within timeout."
}
