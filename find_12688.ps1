$lines = Get-Content "C:\Users\Admin\.gemini\antigravity\brain\5365d727-08e0-4a5f-a070-96ffc1ffee96\.system_generated\logs\transcript_full.jsonl"
for ($i = 2500; $i -ge 0; $i--) {
    if ($lines[$i].Contains('12688')) {
        Write-Host "Matched at $i"
        $lines[$i] | Out-File -FilePath "step_12688.json" -Encoding utf8
        break
    }
}
