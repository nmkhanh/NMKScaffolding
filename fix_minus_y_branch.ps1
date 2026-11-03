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

# Move previous test elements (14047..14053) to -100000
foreach ($id in 14047..14053) {
    Invoke-RestMethod -Uri "$baseUrl/set_parameter" -Method Post -Body (@{elementId=$id; paramName='Offset from Host'; value=-100000} | ConvertTo-Json) -ContentType 'application/json' | Out-Null
}

Write-Host "Placing Family 5 on -Y branch at (37800, -1200) with rot = 270..."
Place-Elem '5' 37800 -1200 480 270 1800 0 900 | Out-Null
Place-Elem '5' 37800 -1200 1830 270 1800 0 900 | Out-Null
Place-Elem '5' 37800 -1200 3630 270 1800 0 900 | Out-Null

Write-Host "Placing Family 1 on -Y branch..."
# Let's test Family 1 with rot = 0 vs rot = 270
$id1 = Place-Elem '1' 36600 -1200 0 270 1800
$id2 = Place-Elem '1' 37800 -1200 0 270 1800

$res = Invoke-RestMethod -Uri "$baseUrl/audit_instances" -Method Post -Body "{}" -ContentType "application/json"
$f1_new = $res.data.instances | Where-Object { $_.id -eq $id1 -or $_.id -eq $id2 }
$f1_new | Select-Object id, family, x, y, z, rotDeg | Format-Table
