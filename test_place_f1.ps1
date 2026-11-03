try {
    $b = @{familyName='1'; x=36600; y=-3000; z=0; rotDeg=90} | ConvertTo-Json
    $res = Invoke-RestMethod -Uri 'http://127.0.0.1:4848/place_instance' -Method Post -Body $b -ContentType 'application/json'
    Write-Host "Success:" ($res | ConvertTo-Json)
} catch {
    Write-Host "Error:" $_.Exception.Message
}
