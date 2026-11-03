# Build Model 2 (-Y Corner Bay) at X = 30000 mm
$sw = [System.Diagnostics.Stopwatch]::StartNew()

$baseUrl = "http://127.0.0.1:4848"

function Post-Json($url, $data) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes(($data | ConvertTo-Json -Compress))
    $req = [System.Net.WebRequest]::CreateHttp($url)
    $req.Method = "POST"
    $req.ContentType = "application/json"
    $req.KeepAlive = $true
    $req.Timeout = 5000
    $req.ContentLength = $bytes.Length
    $stream = $req.GetRequestStream()
    $stream.Write($bytes, 0, $bytes.Length)
    $stream.Close()
    $resp = $req.GetResponse()
    $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
    $respText = $reader.ReadToEnd()
    $reader.Close()
    $resp.Close()
    return ($respText | ConvertFrom-Json)
}

function Place-Elem($fam, $x, $y, $z, $rot=0, $len=0, $w=0, $h=0) {
    $b = @{familyName=$fam; x=$x; y=$y; z=$z; rotDeg=$rot}
    $res = Post-Json "$baseUrl/place_instance" $b
    $id = $res.data.instanceId
    if ($len -gt 0) {
        Post-Json "$baseUrl/set_parameter" @{elementId=$id; paramName='l'; value=$len} | Out-Null
    }
    if ($w -gt 0) {
        Post-Json "$baseUrl/set_parameter" @{elementId=$id; paramName='w'; value=$w} | Out-Null
    }
    if ($h -gt 0) {
        Post-Json "$baseUrl/set_parameter" @{elementId=$id; paramName='h'; value=$h} | Out-Null
    }
    try {
        Post-Json "$baseUrl/set_parameter" @{elementId=$id; paramName='Offset from Host'; value=$z} | Out-Null
    } catch {}
    return $id
}

function Set-ColParams($id, $paramsDict) {
    foreach ($k in $paramsDict.Keys) {
        Post-Json "$baseUrl/set_parameter" @{elementId=$id; paramName=$k; value=$paramsDict[$k]} | Out-Null
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

$x0 = 30000
$xLines = @($x0, ($x0 + 1800), ($x0 + 3000), ($x0 + 4800), ($x0 + 6000), ($x0 + 7800))
$yLines = @(0, -1200)

Write-Host "=== BẮT ĐẦU DỰNG MODEL 2 (-Y CORNER BAY) TẠI X = $x0 ==="
$count = 0

# 1. Base boards (Family 1)
foreach ($x in @($xLines[0], $xLines[1], $xLines[2], $xLines[3])) {
    Place-Elem '1' $x 0 0 0 1800 | Out-Null
    Place-Elem '1' $x -1200 0 0 1800 | Out-Null
    $count += 2
}
# Bay 5:
# At Y = 0 (solid outer side in -Y return): full L = 1800
Place-Elem '1' $xLines[4] 0 0 0 1800 | Out-Null
# At Y = -1200 (connection side): shortened to L = 600 between 36000 and 36600
Place-Elem '1' $xLines[4] -1200 0 0 600 | Out-Null
$count += 2

# 2. Base jacks (Family 2, h = 100) & Columns 2700 & Columns 3600
foreach ($x in $xLines) {
    foreach ($y in $yLines) {
        Place-Elem '2' $x $y 0 0 0 0 100 | Out-Null
        $c1 = Place-Elem '3' $x $y 100 0
        Set-ColParams $c1 $p2700
        $c2 = Place-Elem '3' $x $y 2800 0
        Set-ColParams $c2 $p3600
        $count += 3
    }
}

# 3. Transoms & Ledgers at Ground (Z = 235 mm)
# Transoms (rot = 270, L = 1200) at each X line
foreach ($x in $xLines) {
    Place-Elem '4' $x 0 235 270 1200 | Out-Null
    $count += 1
}
# Longitudinal ledgers (rot = 0)
for ($b = 0; $b -lt 4; $b++) {
    $xSt = $xLines[$b]
    $bLen = $xLines[$b+1] - $xSt
    Place-Elem '4' $xSt 0 235 0 $bLen | Out-Null
    Place-Elem '4' $xSt -1200 235 0 $bLen | Out-Null
    $count += 2
}
# Bay 5 at Ground:
# Y = 0: L = 1800
Place-Elem '4' $xLines[4] 0 235 0 1800 | Out-Null
# Y = -1200: L = 600 (36000 to 36600)
Place-Elem '4' $xLines[4] -1200 235 0 600 | Out-Null
$count += 2

# 4. Mid Ledgers (Z = 930 mm)
for ($b = 0; $b -lt 4; $b++) {
    $xSt = $xLines[$b]
    $bLen = $xLines[$b+1] - $xSt
    Place-Elem '4' $xSt 0 930 0 $bLen | Out-Null
    Place-Elem '4' $xSt -1200 930 0 $bLen | Out-Null
    $count += 2
}
Place-Elem '4' $xLines[4] 0 930 0 1800 | Out-Null
Place-Elem '4' $xLines[4] -1200 930 0 600 | Out-Null
$count += 2

# 5. Sàn Thao Tác 1 (Z = 1830 mm)
# Transoms at each X line
foreach ($x in $xLines) {
    Place-Elem '4' $x 0 1830 270 1200 | Out-Null
    $count += 1
}
# Catwalk Planks (Family 6) & Toe boards (Family 7)
for ($b = 0; $b -lt 5; $b++) {
    $xSt = $xLines[$b]
    $bLen = $xLines[$b+1] - $xSt
    # 2 Planks
    Place-Elem '6' $xSt -345 1830 0 $bLen 500 | Out-Null
    Place-Elem '6' $xSt -855 1830 0 $bLen 500 | Out-Null
    $count += 2
}
# Toe boards along Y = 0:
for ($b = 0; $b -lt 4; $b++) {
    $xSt = $xLines[$b]
    $bLen = $xLines[$b+1] - $xSt
    Place-Elem '7' $xSt -66 1830 0 $bLen | Out-Null
    Place-Elem '7' $xSt -1134 1830 0 $bLen | Out-Null
    $count += 2
}
# Bay 5 Toe boards:
# Y = 0 (solid): L = 1800
Place-Elem '7' $xLines[4] -66 1830 0 1800 | Out-Null
# Y = -1200 (shortened): L = 600
Place-Elem '7' $xLines[4] -1134 1830 0 600 | Out-Null
$count += 2
# End cap toe board at X0 = 30000:
Place-Elem '7' $x0 0 1830 270 1200 | Out-Null
$count += 1

# Guardrails Sàn 1 (Z = 2280 mm & Z = 2730 mm):
# End cap at X0 = 30000:
Place-Elem '4' $x0 0 2280 270 1200 | Out-Null
Place-Elem '4' $x0 0 2730 270 1200 | Out-Null
$count += 2
# Bays 1-4 Guardrails:
for ($b = 0; $b -lt 4; $b++) {
    $xSt = $xLines[$b]
    $bLen = $xLines[$b+1] - $xSt
    Place-Elem '4' $xSt 0 2280 0 $bLen | Out-Null
    Place-Elem '4' $xSt 0 2730 0 $bLen | Out-Null
    Place-Elem '4' $xSt -1200 2280 0 $bLen | Out-Null
    Place-Elem '4' $xSt -1200 2730 0 $bLen | Out-Null
    $count += 4
}
# Bay 5 Guardrails:
# Y = 0: solid L = 1800
Place-Elem '4' $xLines[4] 0 2280 0 1800 | Out-Null
Place-Elem '4' $xLines[4] 0 2730 0 1800 | Out-Null
# Y = -1200: shortened L = 600 (36000 to 36600), OPEN from 36600 to 37800!
Place-Elem '4' $xLines[4] -1200 2280 0 600 | Out-Null
Place-Elem '4' $xLines[4] -1200 2730 0 600 | Out-Null
$count += 4

# 6. Sàn Thao Tác 2 (Z = 3630 mm)
# Transoms
foreach ($x in $xLines) {
    Place-Elem '4' $x 0 3630 270 1200 | Out-Null
    $count += 1
}
# Planks
for ($b = 0; $b -lt 5; $b++) {
    $xSt = $xLines[$b]
    $bLen = $xLines[$b+1] - $xSt
    Place-Elem '6' $xSt -345 3630 0 $bLen 500 | Out-Null
    Place-Elem '6' $xSt -855 3630 0 $bLen 500 | Out-Null
    $count += 2
}
# Toe boards
for ($b = 0; $b -lt 4; $b++) {
    $xSt = $xLines[$b]
    $bLen = $xLines[$b+1] - $xSt
    Place-Elem '7' $xSt -66 3630 0 $bLen | Out-Null
    Place-Elem '7' $xSt -1134 3630 0 $bLen | Out-Null
    $count += 2
}
Place-Elem '7' $xLines[4] -66 3630 0 1800 | Out-Null
Place-Elem '7' $xLines[4] -1134 3630 0 600 | Out-Null
Place-Elem '7' $x0 0 3630 270 1200 | Out-Null
$count += 3

# Guardrails Sàn 2 (Z = 4080 mm & Z = 4530 mm):
Place-Elem '4' $x0 0 4080 270 1200 | Out-Null
Place-Elem '4' $x0 0 4530 270 1200 | Out-Null
$count += 2
for ($b = 0; $b -lt 4; $b++) {
    $xSt = $xLines[$b]
    $bLen = $xLines[$b+1] - $xSt
    Place-Elem '4' $xSt 0 4080 0 $bLen | Out-Null
    Place-Elem '4' $xSt 0 4530 0 $bLen | Out-Null
    Place-Elem '4' $xSt -1200 4080 0 $bLen | Out-Null
    Place-Elem '4' $xSt -1200 4530 0 $bLen | Out-Null
    $count += 4
}
Place-Elem '4' $xLines[4] 0 4080 0 1800 | Out-Null
Place-Elem '4' $xLines[4] 0 4530 0 1800 | Out-Null
Place-Elem '4' $xLines[4] -1200 4080 0 600 | Out-Null
Place-Elem '4' $xLines[4] -1200 4530 0 600 | Out-Null
$count += 4

# End cap guardrails at right end X = 37800 (from Y = 0 to Y = -1200):
Place-Elem '4' $xLines[5] 0 2280 270 1200 | Out-Null
Place-Elem '4' $xLines[5] 0 2730 270 1200 | Out-Null
Place-Elem '4' $xLines[5] 0 4080 270 1200 | Out-Null
Place-Elem '4' $xLines[5] 0 4530 270 1200 | Out-Null
Place-Elem '7' $xLines[5] 0 1830 270 1200 | Out-Null
Place-Elem '7' $xLines[5] 0 3630 270 1200 | Out-Null
$count += 6

# 7. Top Ledgers (Z = 6330 mm)
foreach ($x in $xLines) {
    Place-Elem '4' $x 0 6330 270 1200 | Out-Null
    $count += 1
}
for ($b = 0; $b -lt 4; $b++) {
    $xSt = $xLines[$b]
    $bLen = $xLines[$b+1] - $xSt
    Place-Elem '4' $xSt 0 6330 0 $bLen | Out-Null
    Place-Elem '4' $xSt -1200 6330 0 $bLen | Out-Null
    $count += 2
}
Place-Elem '4' $xLines[4] 0 6330 0 1800 | Out-Null
Place-Elem '4' $xLines[4] -1200 6330 0 600 | Out-Null
$count += 2

# ==============================================================================
# 8. NHỊP RẼ GÓC VUÔNG 90° HƯỚNG -Y ($1800 x $1200 mm)
# ==============================================================================
Write-Host "=== BẮT ĐẦU DỰNG NHỊP RẼ -Y TẠI X in [36600, 37800], Y in [-3000, -1200] ==="

$xIn = $x0 + 6600   # 36600 mm (cột cấy)
$xOut = $x0 + 7800  # 37800 mm (mép ngoài bên phải)
$yTop = -1200       # Giao với hàng cột sau của Main Run
$yBot = -3000       # Vươn về -Y 1800 mm

# Chân móng:
# Ván lót đáy Family 1 (dài 1800 mm dọc Y):
# Note: rot = 270 to extend along -Y
Place-Elem '1' $xIn $yTop 0 270 1800 | Out-Null
Place-Elem '1' $xOut $yTop 0 270 1800 | Out-Null
$count += 2

# Kích chân Family 2 & Cột chống 2700 + 3600:
# Cột cấy P2 (36600, -1200):
Place-Elem '2' $xIn $yTop 0 0 0 0 100 | Out-Null
$c = Place-Elem '3' $xIn $yTop 100 0
Set-ColParams $c $p2700
$c = Place-Elem '3' $xIn $yTop 2800 0
Set-ColParams $c $p3600
# Cột góc ngoài trái P3 (36600, -3000):
Place-Elem '2' $xIn $yBot 0 0 0 0 100 | Out-Null
$c = Place-Elem '3' $xIn $yBot 100 0
Set-ColParams $c $p2700
$c = Place-Elem '3' $xIn $yBot 2800 0
Set-ColParams $c $p3600
# Cột góc ngoài phải P4 (37800, -3000):
Place-Elem '2' $xOut $yBot 0 0 0 0 100 | Out-Null
$c = Place-Elem '3' $xOut $yBot 100 0
Set-ColParams $c $p2700
$c = Place-Elem '3' $xOut $yBot 2800 0
Set-ColParams $c $p3600
# (Lưu ý: Cột P1 tại 37800, -1200 dùng chung trực tiếp từ Main Run)
$count += 9

# Giằng chân nhịp rẽ (Z = 235 mm):
# 2 giằng dọc L = 1800 mm theo -Y (rot = 270)
Place-Elem '4' $xIn $yTop 235 270 1800 | Out-Null
Place-Elem '4' $xOut $yTop 235 270 1800 | Out-Null
# 2 giằng ngang L = 1200 mm theo X (rot = 0)
Place-Elem '4' $xIn $yTop 235 0 1200 | Out-Null
Place-Elem '4' $xIn $yBot 235 0 1200 | Out-Null
$count += 4

# Giằng trung gian h3 (Z = 930 mm):
Place-Elem '4' $xIn $yTop 930 270 1800 | Out-Null
Place-Elem '4' $xOut $yTop 930 270 1800 | Out-Null
$count += 2

# Sàn thao tác 1 (Z = 1830 mm):
# Giằng ngang đỡ mâm tại yTop và yBot:
Place-Elem '4' $xIn $yTop 1830 0 1200 | Out-Null
Place-Elem '4' $xIn $yBot 1830 0 1200 | Out-Null
# 2 Mâm thao tác Family 6 (L = 1800, W = 500, rot = 270):
Place-Elem '6' ($xIn + 345) $yTop 1830 270 1800 500 | Out-Null
Place-Elem '6' ($xOut - 345) $yTop 1830 270 1800 500 | Out-Null
# Ván chắn biên Family 7:
Place-Elem '7' ($xIn + 66) $yTop 1830 270 1800 | Out-Null
Place-Elem '7' ($xOut - 66) $yTop 1830 270 1800 | Out-Null
Place-Elem '7' $xIn $yBot 1830 0 1200 | Out-Null
# Lan can kép Sàn 1:
# Mép trái X = 36600 (rot = 270, L = 1800):
Place-Elem '4' $xIn $yTop 2280 270 1800 | Out-Null
Place-Elem '4' $xIn $yTop 2730 270 1800 | Out-Null
# Mép phải X = 37800 (rot = 270, L = 1800):
Place-Elem '4' $xOut $yTop 2280 270 1800 | Out-Null
Place-Elem '4' $xOut $yTop 2730 270 1800 | Out-Null
# Đầu hồi cuối Y = -3000 (rot = 0, L = 1200):
Place-Elem '4' $xIn $yBot 2280 0 1200 | Out-Null
Place-Elem '4' $xIn $yBot 2730 0 1200 | Out-Null
# LƯU Ý: Tại Y = -1200 từ 36600 đến 37800 KHÔNG CÓ LAN CAN (thông suốt 100%)!
$count += 13

# Sàn thao tác 2 (Z = 3630 mm):
Place-Elem '4' $xIn $yTop 3630 0 1200 | Out-Null
Place-Elem '4' $xIn $yBot 3630 0 1200 | Out-Null
Place-Elem '6' ($xIn + 345) $yTop 3630 270 1800 500 | Out-Null
Place-Elem '6' ($xOut - 345) $yTop 3630 270 1800 500 | Out-Null
Place-Elem '7' ($xIn + 66) $yTop 3630 270 1800 | Out-Null
Place-Elem '7' ($xOut - 66) $yTop 3630 270 1800 | Out-Null
Place-Elem '7' $xIn $yBot 3630 0 1200 | Out-Null
Place-Elem '4' $xIn $yTop 4080 270 1800 | Out-Null
Place-Elem '4' $xIn $yTop 4530 270 1800 | Out-Null
Place-Elem '4' $xOut $yTop 4080 270 1800 | Out-Null
Place-Elem '4' $xOut $yTop 4530 270 1800 | Out-Null
Place-Elem '4' $xIn $yBot 4080 0 1200 | Out-Null
Place-Elem '4' $xIn $yBot 4530 0 1200 | Out-Null
$count += 13

# Giằng đỉnh (Z = 6330 mm):
Place-Elem '4' $xIn $yTop 6330 270 1800 | Out-Null
Place-Elem '4' $xOut $yTop 6330 270 1800 | Out-Null
Place-Elem '4' $xIn $yTop 6330 0 1200 | Out-Null
Place-Elem '4' $xIn $yBot 6330 0 1200 | Out-Null
$count += 4

$sw.Stop()

Write-Host "============================================================"
Write-Host "HOÀN TẤT DỰNG MODEL 2 (-Y CORNER BAY)"
Write-Host "Vị trí: X in [$x0, $($x0 + 7800)], Y in [-3000, 0]"
Write-Host "Nhịp rẽ -Y: X in [$xIn, $xOut], Y in [$yBot, $yTop]"
Write-Host "Tổng số cấu kiện đã đặt: $count"
Write-Host "Thời gian thực thi: $($sw.ElapsedMilliseconds) ms ($([math]::Round($sw.Elapsed.TotalSeconds, 2)) s)"
Write-Host "============================================================"
