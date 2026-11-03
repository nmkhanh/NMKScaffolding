$proc = Get-Process -Id 15480
$uiThread = $proc.Threads | Sort-Object TotalProcessorTime -Descending | Select-Object -First 5
foreach ($t in $uiThread) {
    Write-Host "Thread ID: $($t.Id) | State: $($t.ThreadState) | WaitReason: $($t.WaitReason) | CPU: $($t.TotalProcessorTime)"
}
