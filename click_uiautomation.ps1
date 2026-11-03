Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Write-Host "Searching for Revit modal dialogs via UI Automation..."
$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ClassNameProperty, "#32770"
)
$dialogs = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
    [System.Windows.Automation.TreeScope]::Children, $cond
)

Write-Host "Found $($dialogs.Count) #32770 dialogs"
foreach ($dlg in $dialogs) {
    $name = $dlg.Current.Name
    Write-Host "Dialog Name: '$name'"
    if ($name -like "*Revit*") {
        Write-Host "Target dialog found! Searching for OK button..."
        $allBtns = $dlg.FindAll(
            [System.Windows.Automation.TreeScope]::Descendants,
            [System.Windows.Automation.Condition]::TrueCondition
        )
        foreach ($el in $allBtns) {
            $elName = $el.Current.Name
            $elType = $el.Current.ControlType.ProgrammaticName
            $elId = $el.Current.AutomationId
            if ($elName -eq "OK" -or $elId -eq "CommandLink_1" -or $elId -eq "1" -or $elName -eq "&OK") {
                Write-Host "Found button: '$elName' (ID: $elId, Type: $elType). Invoking click..."
                try {
                    $pattern = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
                    $pattern.Invoke()
                    Write-Host "SUCCESSFULLY CLICKED VIA UI AUTOMATION!"
                } catch {
                    Write-Host "Invoke failed: $($_.Exception.Message)"
                }
            }
        }
    }
}
