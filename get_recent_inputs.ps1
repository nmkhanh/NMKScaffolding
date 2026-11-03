$lines = [System.IO.File]::ReadAllLines("C:\Users\Admin\.gemini\antigravity\brain\5365d727-08e0-4a5f-a070-96ffc1ffee96\.system_generated\logs\transcript.jsonl", [System.Text.Encoding]::UTF8)
for ($i = $lines.Count - 50; $i -lt $lines.Count; $i++) {
    $l = $lines[$i]
    if ($l -like '*"type":"USER_INPUT"*' -or $l -like '*"source":"MODEL"*') {
        $obj = $l | ConvertFrom-Json
        if ($obj.content -and $obj.content.Length -gt 0) {
            Write-Host "[$i] $($obj.type) / $($obj.source)"
            $txt = $obj.content
            if ($txt.Length -gt 400) { $txt = $txt.Substring(0, 400) + "... [TRUNCATED]" }
            Write-Host $txt
            Write-Host "--------------------------------------------------------"
        }
    }
}
