Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$procId = 25324
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $procId)
$elements = [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)

Write-Host "Elements found for PID $procId : $($elements.Count)"
foreach ($e in $elements) {
    $autoId = $e.Current.AutomationId
    $name = $e.Current.Name
    if ($autoId -eq "CommandLink_1001" -or $autoId -eq "CommandLink_1002" -or $name -like "*Always Load*") {
        Write-Host "Found button: $name ($autoId). Invoking..."
        $inv = $e.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $inv.Invoke()
        Write-Host "INVOKED!"
        break
    }
}
