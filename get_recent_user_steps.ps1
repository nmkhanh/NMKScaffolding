
$path = 'C:\Users\Admin\.gemini\antigravity\brain\5365d727-08e0-4a5f-a070-96ffc1ffee96\.system_generated\logs\transcript.jsonl'
$lines = [System.IO.File]::ReadAllLines($path)
$userSteps = @()
foreach ($line in $lines) {
    if ($line.Contains('"USER_INPUT"')) {
        $userSteps += $line
    }
}
$lastSteps = $userSteps | Select-Object -Last 10
foreach ($s in $lastSteps) {
    $obj = $s | ConvertFrom-Json
    [Console]::WriteLine("=== STEP " + $obj.step_index + " ===")
    [Console]::WriteLine($obj.content)
}
