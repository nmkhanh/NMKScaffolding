$lines = [System.IO.File]::ReadAllLines('C:\Users\Admin\.gemini\antigravity\brain\5365d727-08e0-4a5f-a070-96ffc1ffee96\.system_generated\logs\transcript.jsonl', [System.Text.Encoding]::UTF8)
for ($i = 3995; $i -le 4025; $i++) {
    $line = $lines[$i]
    if ($line.Contains('"type":"USER_INPUT"')) {
        Write-Host "USER_INPUT: " $line
    }
}
