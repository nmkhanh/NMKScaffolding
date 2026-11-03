$baseUrl = "http://127.0.0.1:4848"

function Place-Elem($fam, $x, $y, $z, $rot=0, $len=0, $w=0, $h=0) {
    $b = @{familyName=$fam; x=$x; y=$y; z=$z; rotDeg=$rot} | ConvertTo-Json
    $res = Invoke-RestMethod -Uri "$baseUrl/place_instance" -Method Post -Body $b -ContentType 'application/json'
    $id = $res.data.instanceId
    if ($len -gt 0) {
        Invoke-RestMethod -Uri "$baseUrl/set_parameter" -Method Post -Body (@{elementId=$id; paramName='l'; value=$len} | ConvertTo-Json) -ContentType 'application/json' | Out-Null
    }
    if ($w -gt 0) {
        Invoke-RestMethod -Uri "$baseUrl/set_parameter" -Method Post -Body (@{elementId=$id; paramName='w'; value=$w} | ConvertTo-Json) -ContentType 'application/json' | Out-Null
    }
    if ($h -gt 0) {
        Invoke-RestMethod -Uri "$baseUrl/set_parameter" -Method Post -Body (@{elementId=$id; paramName='h'; value=$h} | ConvertTo-Json) -ContentType 'application/json' | Out-Null
    }
    try {
        Invoke-RestMethod -Uri "$baseUrl/set_parameter" -Method Post -Body (@{elementId=$id; paramName='Offset from Host'; value=$z} | ConvertTo-Json) -ContentType 'application/json' | Out-Null
    } catch {}
    return $id
}

Write-Host "1. Setting length for 14047..."
Invoke-RestMethod -Uri "$baseUrl/set_parameter" -Method Post -Body (@{elementId=14047; paramName='l'; value=1800} | ConvertTo-Json) -ContentType 'application/json' | Out-Null

Write-Host "2. Placing 2nd Family 1 board at (37800, -3000)..."
Place-Elem '1' 37800 -3000 0 90 1800 | Out-Null

Write-Host "3. Placing 3 Family 5 diagonal braces on -Y branch (X = 37800)..."
Place-Elem '5' 37800 -3000 480 90 1800 0 900 | Out-Null
Place-Elem '5' 37800 -3000 1830 90 1800 0 900 | Out-Null
Place-Elem '5' 37800 -3000 3630 90 1800 0 900 | Out-Null

Write-Host "4. Placing 15 Family 5 diagonal braces on 5-bay main run (Y = 0)..."
$bays = @(
    @{x=30000; l=1800},
    @{x=31800; l=1200},
    @{x=33000; l=1800},
    @{x=34800; l=1200},
    @{x=36000; l=1800}
)
foreach ($b in $bays) {
    Place-Elem '5' $b.x 0 480 0 $b.l 0 900 | Out-Null
    Place-Elem '5' $b.x 0 1830 0 $b.l 0 900 | Out-Null
    Place-Elem '5' $b.x 0 3630 0 $b.l 0 900 | Out-Null
}

Write-Host "5. Moving scratch elements 13865, 13866 to -100000..."
Invoke-RestMethod -Uri "$baseUrl/set_parameter" -Method Post -Body (@{elementId=13865; paramName='Offset from Host'; value=-100000} | ConvertTo-Json) -ContentType 'application/json' | Out-Null
Invoke-RestMethod -Uri "$baseUrl/set_parameter" -Method Post -Body (@{elementId=13866; paramName='Offset from Host'; value=-100000} | ConvertTo-Json) -ContentType 'application/json' | Out-Null

Write-Host "ALL ELEMENTS PLACED SUCCESSFULLY!"
