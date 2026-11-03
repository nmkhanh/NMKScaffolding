Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$windows = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
    [System.Windows.Automation.TreeScope]::Children,
    [System.Windows.Automation.Condition]::TrueCondition
)

Write-Host "Total top-level elements: $($windows.Count)"
foreach ($w in $windows) {
    $name = $w.Current.Name
    $cls = $w.Current.ClassName
    $procId = $w.Current.ProcessId
    if ($procId -eq 15480 -or $name -like "*Revit*" -or $name -like "*Warning*") {
        Write-Host "HWND/ID: $procId | Class: $cls | Name: '$name'"
    }
}
