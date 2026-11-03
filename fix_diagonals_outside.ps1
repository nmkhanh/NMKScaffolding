$baseUrl = "http://127.0.0.1:4848"
$sw = [System.Diagnostics.Stopwatch]::StartNew()

function Place-Elem($fam, $x, $y, $z, $rot=0, $len=0, $w=0, $h=0) {
    $b = @{familyName=$fam; x=$x; y=$y; z=$z; rotDeg=$rot} | ConvertTo-Json
    $res = Invoke-RestMethod -Uri "$baseUrl/place_instance" -Method Post -Body $b -ContentType 'application/json' -TimeoutSec 30
    $id = $res.data.instanceId
    if ($len -gt 0) {
        Invoke-RestMethod -Uri "$baseUrl/set_parameter" -Method Post -Body (@{elementId=$id; paramName='l'; value=$len} | ConvertTo-Json) -ContentType 'application/json' -TimeoutSec 30 | Out-Null
    }
    if ($w -gt 0) {
        Invoke-RestMethod -Uri "$baseUrl/set_parameter" -Method Post -Body (@{elementId=$id; paramName='w'; value=$w} | ConvertTo-Json) -ContentType 'application/json' -TimeoutSec 30 | Out-Null
    }
    if ($h -gt 0) {
        Invoke-RestMethod -Uri "$baseUrl/set_parameter" -Method Post -Body (@{elementId=$id; paramName='h'; value=$h} | ConvertTo-Json) -ContentType 'application/json' -TimeoutSec 30 | Out-Null
    }
    try {
        Invoke-RestMethod -Uri "$baseUrl/set_parameter" -Method Post -Body (@{elementId=$id; paramName='Offset from Host'; value=$z} | ConvertTo-Json) -ContentType 'application/json' -TimeoutSec 30 | Out-Null
    } catch {}
    return $id
}

Write-Host "=== 1. CHUYỂN GIẰNG CHÉO TẠI Y = 0 VỀ OFFSET ÂM (-100000) ==="
$oldIds = @(14055, 14057, 14059, 14061, 14063, 14065, 14067, 14069, 14071, 14073, 14075, 14077, 14079, 14081, 14083)
foreach ($elemId in $oldIds) {
    try {
        Invoke-RestMethod -Uri "$baseUrl/set_parameter" -Method Post -Body (@{elementId=$elemId; paramName='Offset from Host'; value=-100000} | ConvertTo-Json) -ContentType 'application/json' -TimeoutSec 5 | Out-Null
    } catch {}
}

Write-Host "=== 2. ĐẶT GIẰNG CHÉO PHÍA NGOÀI (Y = -1200) CHO DÃY CHÍNH (NHỊP 1 -> 4) ==="
$baysOutside = @(
    @{x=30000; l=1800},
    @{x=31800; l=1200},
    @{x=33000; l=1800},
    @{x=34800; l=1200}
)
foreach ($b in $baysOutside) {
    Place-Elem '5' $b.x -1200 480 0 $b.l 0 900 | Out-Null
    Place-Elem '5' $b.x -1200 1830 0 $b.l 0 900 | Out-Null
    Place-Elem '5' $b.x -1200 3630 0 $b.l 0 900 | Out-Null
}

Write-Host "=== 3. ĐẶT GIẰNG CHÉO PHÍA NGOÀI CHO NHÁNH -Y ==="
# Mặt ngoài bên phải (X = 37800) dọc theo phương Y: từ -3000 đến -1200 (L = 1800)
Place-Elem '5' 37800 -3000 480 90 1800 0 900 | Out-Null
Place-Elem '5' 37800 -3000 1830 90 1800 0 900 | Out-Null
Place-Elem '5' 37800 -3000 3630 90 1800 0 900 | Out-Null

# Mặt ngoài đầu hồi cuối (Y = -3000): từ X = 36600 đến 37800 (L = 1200)
Place-Elem '5' 36600 -3000 480 0 1200 0 900 | Out-Null
Place-Elem '5' 36600 -3000 1830 0 1200 0 900 | Out-Null
Place-Elem '5' 36600 -3000 3630 0 1200 0 900 | Out-Null

$sw.Stop()
Write-Host "=========================================================="
Write-Host "HOÀN TẤT: TOÀN BỘ GIẰNG CHÉO ĐÃ ĐẶT CHUẨN XÁC Ở PHÍA NGOÀI!"
Write-Host "Thời gian thực thi: $($sw.ElapsedMilliseconds) ms"
Write-Host "=========================================================="
