Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Write-Host "Searching for TaskDialog Security Unsigned Add-In..."
$condition = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ClassNameProperty, "#32770"
)
$dialogs = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
    [System.Windows.Automation.TreeScope]::Children, $condition
)

foreach ($dlg in $dialogs) {
    $name = $dlg.Current.Name
    Write-Host "Found Dialog: $name"
    if ($name -like "*Security*" -or $name -like "*Add-In*" -or $name -like "*Publisher*") {
        Write-Host "Target security dialog found! Searching for CommandLink_1001 (Always Load)..."
        
        $btnCond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::AutomationIdProperty, "CommandLink_1001"
        )
        $btn = $dlg.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
        
        if (-not $btn) {
            # Try CommandLink_1002
            $btnCond = New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::AutomationIdProperty, "CommandLink_1002"
            )
            $btn = $dlg.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
        }

        if ($btn) {
            Write-Host "Found button: $($btn.Current.Name) ($($btn.Current.AutomationId)). Clicking..."
            $invoke = $btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
            $invoke.Invoke()
            Write-Host "CLICKED SUCCESSFULLY!"
        } else {
            Write-Host "Could not find CommandLink_1001 or 1002 inside dialog."
        }
    }
}
