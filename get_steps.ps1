$lines = Get-Content 'C:\Users\Admin\.gemini\antigravity\brain\5365d727-08e0-4a5f-a070-96ffc1ffee96\.system_generated\logs\transcript_full.jsonl'
foreach ($l in $lines) {
    if ($l -match '"step_index":(2781|2782|2783|2813|2819),') {
        $j = $l | ConvertFrom-Json
        Write-Host "=== STEP $($j.step_index) ==="
        Write-Host "THINKING:" $j.thinking
        Write-Host "CONTENT:" $j.content
    }
}
