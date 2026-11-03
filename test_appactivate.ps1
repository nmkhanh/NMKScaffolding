$wshell = New-Object -ComObject WScript.Shell
$res = $wshell.AppActivate(15480)
Write-Host "AppActivate(15480) = $res"
if ($res) {
    Start-Sleep -Milliseconds 200
    $wshell.SendKeys("{ESC}")
    Write-Host "Sent ESC via WScript.Shell!"
}
