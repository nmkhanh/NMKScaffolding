$sw = [System.Diagnostics.Stopwatch]::StartNew()

function Place-Elem($fam, $x, $y, $z, $rot=0, $len=0, $w=0, $h=0) {
    $b = @{familyName=$fam; x=$x; y=$y; z=$z; rotDeg=$rot} | ConvertTo-Json
    $res = Invoke-RestMethod -Uri 'http://127.0.0.1:4848/place_instance' -Method Post -Body $b -ContentType 'application/json'
    $id = $res.data.instanceId
    if ($len -gt 0) {
        Invoke-RestMethod -Uri 'http://127.0.0.1:4848/set_parameter' -Method Post -Body (@{elementId=$id; paramName='l'; value=$len} | ConvertTo-Json) -ContentType 'application/json' | Out-Null
    }
    if ($w -gt 0) {
        Invoke-RestMethod -Uri 'http://127.0.0.1:4848/set_parameter' -Method Post -Body (@{elementId=$id; paramName='w'; value=$w} | ConvertTo-Json) -ContentType 'application/json' | Out-Null
    }
    if ($h -gt 0) {
        Invoke-RestMethod -Uri 'http://127.0.0.1:4848/set_parameter' -Method Post -Body (@{elementId=$id; paramName='h'; value=$h} | ConvertTo-Json) -ContentType 'application/json' | Out-Null
    }
    try {
        Invoke-RestMethod -Uri 'http://127.0.0.1:4848/set_parameter' -Method Post -Body (@{elementId=$id; paramName='Offset from Host'; value=$z} | ConvertTo-Json) -ContentType 'application/json' | Out-Null
    } catch {}
    return $id
}

function Set-ColParams($id, $paramsDict) {
    foreach ($k in $paramsDict.Keys) {
        Invoke-RestMethod -Uri 'http://127.0.0.1:4848/set_parameter' -Method Post -Body (@{elementId=$id; paramName=$k; value=$paramsDict[$k]} | ConvertTo-Json) -ContentType 'application/json' | Out-Null
    }
}

$p2700 = [ordered]@{
    h=2700; h1=135; h2=380; h3=830; h4=1280; h5=1730; h6=2180; h7=2630;
    h8=0; h9=0; h10=0; h11=0; h12=0; h13=0; h14=0; h15=0
}
$p3600 = [ordered]@{
    h=3600; h1=380; h2=830; h3=1280; h4=1730; h5=2180; h6=2630; h7=3080; h8=3530;
    h9=0; h10=0; h11=0; h12=0; h13=0; h14=0; h15=0
}

$x0 = 15000
$xLines = @($x0, $x0+1800, $x0+3000, $x0+4800, $x0+6000, $x0+7800)
$bayLens = @(1800, 1200, 1800, 1200, 1800)

$count = 0

# --- A. DỰNG 5 NHỊP CHÍNH (Offset X = 15000) ---
# 1. Ván lót đáy Family 1:
for ($i = 0; $i -lt 5; $i++) {
    $xStart = $xLines[$i]
    $len = $bayLens[$i]
    Place-Elem '1' $xStart 0 0 0 $len | Out-Null
    if ($i -eq 4) {
        # Bay 5 dọc Y = -1200 thu ngắn về 600 mm!
        Place-Elem '1' $xStart -1200 0 0 600 | Out-Null
    } else {
        Place-Elem '1' $xStart -1200 0 0 $len | Out-Null
    }
    $count += 2
}

# 2. 12 Kích chân & 24 Cột chống cho 6 hàng cột chính:
foreach ($x in $xLines) {
    foreach ($y in @(0, -1200)) {
        Place-Elem '2' $x $y 0 0 0 0 100 | Out-Null
        $c1 = Place-Elem '3' $x $y 100
        Set-ColParams $c1 $p2700
        $c2 = Place-Elem '3' $x $y 2800
        Set-ColParams $c2 $p3600
        $count += 3
    }
}

# 3. Giằng ngang đỡ mâm / giằng chân / giằng đỉnh tại 6 trục cột (X_const):
foreach ($x in $xLines) {
    Place-Elem '4' $x 0 235 270 1200 | Out-Null
    Place-Elem '4' $x 0 1830 270 1200 | Out-Null
    Place-Elem '4' $x 0 3630 270 1200 | Out-Null
    Place-Elem '4' $x 0 6330 270 1200 | Out-Null
    $count += 4
}

# 4. Đóng đầu hồi ngoài cùng tại X = 15000:
Place-Elem '4' $x0 0 2280 270 1200 | Out-Null
Place-Elem '4' $x0 0 2730 270 1200 | Out-Null
Place-Elem '4' $x0 0 4080 270 1200 | Out-Null
Place-Elem '4' $x0 0 4530 270 1200 | Out-Null
Place-Elem '7' $x0 0 1830 270 1200 | Out-Null
Place-Elem '7' $x0 0 3630 270 1200 | Out-Null
$count += 6

# 5. Đóng đầu hồi nhịp 5 tại X = 22800 (trên đoạn Y in [-1200, 0]):
$xEnd = $xLines[5]
Place-Elem '4' $xEnd 0 2280 270 1200 | Out-Null
Place-Elem '4' $xEnd 0 2730 270 1200 | Out-Null
Place-Elem '4' $xEnd 0 4080 270 1200 | Out-Null
Place-Elem '4' $xEnd 0 4530 270 1200 | Out-Null
Place-Elem '7' $xEnd 0 1830 270 1200 | Out-Null
Place-Elem '7' $xEnd 0 3630 270 1200 | Out-Null
$count += 6

# 6. Dọc các nhịp chính 1 đến 5:
for ($i = 0; $i -lt 5; $i++) {
    $xStart = $xLines[$i]
    $len = $bayLens[$i]
    
    # Giằng chân:
    Place-Elem '4' $xStart 0 235 0 $len | Out-Null
    $count++
    if ($i -eq 4) {
        Place-Elem '4' $xStart -1200 235 0 600 | Out-Null
        $count++
    } else {
        Place-Elem '4' $xStart -1200 235 0 $len | Out-Null
        $count++
    }
    
    # Giằng giữa h3 (Z = 930):
    Place-Elem '4' $xStart 0 930 0 $len | Out-Null
    $count++
    if ($i -eq 4) {
        Place-Elem '4' $xStart -1200 930 0 600 | Out-Null
        $count++
    }
    
    # Lan can dọc mặt trước Y = 0 (cả 5 nhịp):
    Place-Elem '4' $xStart 0 2280 0 $len | Out-Null
    Place-Elem '4' $xStart 0 2730 0 $len | Out-Null
    Place-Elem '4' $xStart 0 4080 0 $len | Out-Null
    Place-Elem '4' $xStart 0 4530 0 $len | Out-Null
    $count += 4
    
    # Lan can dọc mặt sau Y = -1200 (chỉ đoạn 600 mm của Bay 5):
    if ($i -eq 4) {
        Place-Elem '4' $xStart -1200 2280 0 600 | Out-Null
        Place-Elem '4' $xStart -1200 2730 0 600 | Out-Null
        Place-Elem '4' $xStart -1200 4080 0 600 | Out-Null
        Place-Elem '4' $xStart -1200 4530 0 600 | Out-Null
        $count += 4
    }
    
    # Giằng đỉnh (Z = 6330):
    Place-Elem '4' $xStart 0 6330 0 $len | Out-Null
    $count++
    if ($i -eq 4) {
        Place-Elem '4' $xStart -1200 6330 0 600 | Out-Null
        $count++
    } else {
        Place-Elem '4' $xStart -1200 6330 0 $len | Out-Null
        $count++
    }
    
    # Mâm sàn thao tác:
    Place-Elem '6' $xStart -345 1830 0 $len 500 | Out-Null
    Place-Elem '6' $xStart -855 1830 0 $len 500 | Out-Null
    Place-Elem '6' $xStart -345 3630 0 $len 500 | Out-Null
    Place-Elem '6' $xStart -855 3630 0 $len 500 | Out-Null
    $count += 4
    
    # Ván chắn biên:
    Place-Elem '7' $xStart -66 1830 0 $len | Out-Null
    Place-Elem '7' $xStart -66 3630 0 $len | Out-Null
    $count += 2
    if ($i -eq 4) {
        Place-Elem '7' $xStart -1134 1830 0 600 | Out-Null
        Place-Elem '7' $xStart -1134 3630 0 600 | Out-Null
        $count += 2
    } else {
        Place-Elem '7' $xStart -1134 1830 0 $len | Out-Null
        Place-Elem '7' $xStart -1134 3630 0 $len | Out-Null
        $count += 2
    }
    
    # Giằng chéo Family 5 (mặt sau Y = -1200 cho nhịp 1..4):
    if ($i -lt 4) {
        Place-Elem '5' $xStart -1200 480 0 $len 0 900 | Out-Null
        Place-Elem '5' $xStart -1200 1830 0 $len 0 900 | Out-Null
        Place-Elem '5' $xStart -1200 3630 0 $len 0 900 | Out-Null
        $count += 3
    }
}

# --- B. DỰNG NHỊP RẼ VUÔNG GÓC -Y (X in [21600, 22800], Y in [-3000, -1200]) ---
$xIn = $x0 + 6600   # 21600
$xOut = $x0 + 7800  # 22800
$yTop = -1200
$yBot = -3000

# 1. Ván lót đáy Family 1 (2 tấm dọc Y, dài 1800, rot=270):
# Công thức target (X, Y) -> input (-Y, X)
Place-Elem '1' 1200 $xIn 0 270 1800 | Out-Null
Place-Elem '1' 1200 $xOut 0 270 1800 | Out-Null
$count += 2

# 2. 3 Cụm cột mới tại (21600, -1200), (21600, -3000), (22800, -3000):
$cornerPosts = @(
    @{x=$xIn; y=$yTop},
    @{x=$xIn; y=$yBot},
    @{x=$xOut; y=$yBot}
)
foreach ($cp in $cornerPosts) {
    Place-Elem '2' $cp.x $cp.y 0 0 0 0 100 | Out-Null
    $c1 = Place-Elem '3' $cp.x $cp.y 100
    Set-ColParams $c1 $p2700
    $c2 = Place-Elem '3' $cp.x $cp.y 2800
    Set-ColParams $c2 $p3600
    $count += 3
}

# 3. Giằng chân (Z = 235 mm):
Place-Elem '4' $xIn $yTop 235 270 1800 | Out-Null
Place-Elem '4' $xOut $yTop 235 270 1800 | Out-Null
Place-Elem '4' $xIn $yBot 235 0 1200 | Out-Null
Place-Elem '4' $xIn $yTop 235 0 1200 | Out-Null
$count += 4

# 4. Giằng trung gian h3 (Z = 930 mm):
Place-Elem '4' $xIn $yTop 930 270 1800 | Out-Null
Place-Elem '4' $xOut $yTop 930 270 1800 | Out-Null
$count += 2

# 5. Giằng chéo Family 5 (mặt ngoài X = 22800):
Place-Elem '5' $xOut $yTop 480 270 1800 0 900 | Out-Null
Place-Elem '5' $xOut $yTop 1830 270 1800 0 900 | Out-Null
Place-Elem '5' $xOut $yTop 3630 270 1800 0 900 | Out-Null
$count += 3

# 6. Sàn thao tác 1 (Z = 1830 mm):
Place-Elem '4' $xIn $yTop 1830 0 1200 | Out-Null
Place-Elem '4' $xIn $yBot 1830 0 1200 | Out-Null
Place-Elem '6' ($xIn + 345) $yTop 1830 270 1800 500 | Out-Null
Place-Elem '6' ($xOut - 345) $yTop 1830 270 1800 500 | Out-Null
Place-Elem '7' ($xIn + 66) $yTop 1830 270 1800 | Out-Null
Place-Elem '7' ($xOut - 66) $yTop 1830 270 1800 | Out-Null
Place-Elem '7' $xIn $yBot 1830 0 1200 | Out-Null
# Lan can kép Sàn 1:
Place-Elem '4' $xIn $yTop 2280 270 1800 | Out-Null
Place-Elem '4' $xIn $yTop 2730 270 1800 | Out-Null
Place-Elem '4' $xOut $yTop 2280 270 1800 | Out-Null
Place-Elem '4' $xOut $yTop 2730 270 1800 | Out-Null
Place-Elem '4' $xIn $yBot 2280 0 1200 | Out-Null
Place-Elem '4' $xIn $yBot 2730 0 1200 | Out-Null
$count += 13

# 7. Sàn thao tác 2 (Z = 3630 mm):
Place-Elem '4' $xIn $yTop 3630 0 1200 | Out-Null
Place-Elem '4' $xIn $yBot 3630 0 1200 | Out-Null
Place-Elem '6' ($xIn + 345) $yTop 3630 270 1800 500 | Out-Null
Place-Elem '6' ($xOut - 345) $yTop 3630 270 1800 500 | Out-Null
Place-Elem '7' ($xIn + 66) $yTop 3630 270 1800 | Out-Null
Place-Elem '7' ($xOut - 66) $yTop 3630 270 1800 | Out-Null
Place-Elem '7' $xIn $yBot 3630 0 1200 | Out-Null
# Lan can kép Sàn 2:
Place-Elem '4' $xIn $yTop 4080 270 1800 | Out-Null
Place-Elem '4' $xIn $yTop 4530 270 1800 | Out-Null
Place-Elem '4' $xOut $yTop 4080 270 1800 | Out-Null
Place-Elem '4' $xOut $yTop 4530 270 1800 | Out-Null
Place-Elem '4' $xIn $yBot 4080 0 1200 | Out-Null
Place-Elem '4' $xIn $yBot 4530 0 1200 | Out-Null
$count += 13

# 8. Giằng đỉnh (Z = 6330 mm):
Place-Elem '4' $xIn $yTop 6330 270 1800 | Out-Null
Place-Elem '4' $xOut $yTop 6330 270 1800 | Out-Null
Place-Elem '4' $xIn $yTop 6330 0 1200 | Out-Null
Place-Elem '4' $xIn $yBot 6330 0 1200 | Out-Null
$count += 4

$sw.Stop()
[PSCustomObject]@{
    model = "Model 2 (-Y Corner Return)"
    offsetLocation = "X = $x0 to $($x0 + 7800)"
    cornerBay = "X: [$xIn, $xOut], Y: [$yBot, $yTop]"
    placedElements = $count
    elapsedMs = $sw.ElapsedMilliseconds
} | Format-List
