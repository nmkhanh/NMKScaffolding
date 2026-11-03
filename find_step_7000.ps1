$lines = [System.IO.File]::ReadAllLines("C:\Users\Admin\.gemini\antigravity\brain\5365d727-08e0-4a5f-a070-96ffc1ffee96\.system_generated\logs\transcript.jsonl", [System.Text.Encoding]::UTF8)
for ($i = 6900; $i -lt 7100; $i++) {
    if ($i -ge $lines.Count) { break }
    $l = $lines[$i]
    if ($l -like '*PLANNER_RESPONSE*' -or $l -like '*"type":"USER_INPUT"*') {
        $obj = $l | ConvertFrom-Json
        if ($obj.content -and $obj.content.Length -gt 0) {
            Write-Host "[$i] $($obj.type)"
            $txt = $obj.content
            if ($txt.Length -gt 500) { $txt = $txt.Substring(0, 500) + "... [TRUNCATED]" }
            Write-Host $txt
            Write-Host "--------------------------------------------------------"
        }
    }
}
