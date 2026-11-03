$lines = Get-Content "C:\Users\Admin\.gemini\antigravity\brain\5365d727-08e0-4a5f-a070-96ffc1ffee96\.system_generated\logs\transcript.jsonl"
foreach ($line in $lines) {
    if ($line.Contains('"source":"USER_EXPLICIT"')) {
        $j = $line | ConvertFrom-Json
        Write-Host "Step $($j.step_index): $($j.content)"
    }
}
