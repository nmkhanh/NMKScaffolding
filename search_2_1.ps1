$lines = [System.IO.File]::ReadAllLines('C:\Users\Admin\.gemini\antigravity\brain\5365d727-08e0-4a5f-a070-96ffc1ffee96\.system_generated\logs\transcript.jsonl', [System.Text.Encoding]::UTF8)
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i].Contains('2.1')) {
        $sub = $lines[$i]
        if ($sub.Length -gt 250) { $sub = $sub.Substring(0, 250) }
        Write-Host ($i.ToString() + ": " + $sub)
    }
}
