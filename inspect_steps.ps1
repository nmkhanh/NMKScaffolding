$lines = Get-Content "C:\Users\Admin\.gemini\antigravity\brain\5365d727-08e0-4a5f-a070-96ffc1ffee96\.system_generated\logs\transcript_full.jsonl"
for ($i = 2040; $i -lt 2065 -and $i -lt $lines.Count; $i++) {
    $obj = $lines[$i] | ConvertFrom-Json
    if ($obj.source -eq "USER_EXPLICIT" -or $obj.type -eq "PLANNER_RESPONSE") {
        Write-Host "--- STEP $($obj.step_index) ($($obj.source)) ---"
        if ($obj.content) {
            $len = [math]::Min(400, $obj.content.Length)
            Write-Host ($obj.content.Substring(0, $len))
        }
    }
}
