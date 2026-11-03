for ($i = 0; $i -lt 120; $i++) {
    Start-Sleep -Seconds 2
    try {
        $res = Invoke-RestMethod -Uri "http://127.0.0.1:4848/status" -Method Get -TimeoutSec 1 -ErrorAction Stop
        if ($res.status -eq "online") {
            Start-Sleep -Seconds 3
            $payload = @{ action = "generate_scaffolding" } | ConvertTo-Json
            $result = Invoke-RestMethod -Uri "http://127.0.0.1:4848/action" -Method Post -Body $payload -ContentType "application/json" -TimeoutSec 90
            Set-Content -Path "c:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\revit_2022_mcp_bridge\last_run.json" -Value ($result | ConvertTo-Json -Depth 5)
            break
        }
    } catch {}
}
