# ==============================================================================
# SCRIPT DỰNG TRƯỜNG HỢP 1.0 (5 NHỊP TIÊU CHUẨN 1800-1200-1800-1200-1800 MM)
# THỰC THI 100% QUA MCP BRIDGE (PORT 4848) - BÁM SÁT 13 BƯỚC CỦA 1800x1200.md
# ==============================================================================
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$baseUrl = "http://127.0.0.1:4848"

# Bật tối đa kết nối song song và tái sử dụng KeepAlive
[System.Net.ServicePointManager]::DefaultConnectionLimit = 50
[System.Net.ServicePointManager]::Expect100Continue = $false

function Post-Json($url, $data) {
    $json = $data | ConvertTo-Json -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $req = [System.Net.WebRequest]::CreateHttp($url)
    $req.Method = "POST"
    $req.ContentType = "application/json"
    $req.KeepAlive = $true
    $req.Timeout = 10000
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

# Tọa độ Trường hợp 1.0 đặt tại gốc X = 0
$x0 = 0
$xLines = @(0, 1800, 3000, 4800, 6000, 7800)
$bayLens = @(1800, 1200, 1800, 1200, 1800)
$count = 0

Write-Host "=== BẮT ĐẦU DỰNG TRƯỜNG HỢP 1.0 (5 NHỊP CHUẨN ALINCO) QUA MCP ==="

# 1. BƯỚC 1: Ván lót đáy Family 1 tại Z = 0 (10 tấm)
for ($i = 0; $i -lt 5; $i++) {
    $xSt = $xLines[$i]
    $len = $bayLens[$i]
    Place-Elem '1' $xSt 0 0 0 $len | Out-Null
    Place-Elem '1' $xSt -1200 0 0 $len | Out-Null
    $count += 2
}

# 2. BƯỚC 2 & 3: 12 Kích chân (Family 2, h = 100) & 24 Cột chống ALINCO 2700 + 3600 (Family 3)
foreach ($x in $xLines) {
    foreach ($y in @(0, -1200)) {
        Place-Elem '2' $x $y 0 0 0 0 100 | Out-Null
        $c1 = Place-Elem '3' $x $y 100 0
        Set-ColParams $c1 $p2700
        $c2 = Place-Elem '3' $x $y 2800 0
        Set-ColParams $c2 $p3600
        $count += 3
    }
}

# 3. BƯỚC 4: Giằng ngang chân & giằng ngang các tầng (6 trục cột)
foreach ($x in $xLines) {
    Place-Elem '4' $x 0 235 270 1200 | Out-Null   # Giằng chân (h1)
    Place-Elem '4' $x 0 1830 270 1200 | Out-Null  # Sàn 1 đỡ mâm (h5 cột 2700)
    Place-Elem '4' $x 0 3630 270 1200 | Out-Null  # Sàn 2 đỡ mâm (h2 cột 3600)
    Place-Elem '4' $x 0 6330 270 1200 | Out-Null  # Giằng đỉnh (h8 cột 3600)
    $count += 4
}

# 4. Đóng đầu hồi ngoài cùng tại X = 0 (Nhịp 1)
Place-Elem '4' $x0 0 2280 270 1200 | Out-Null # Lan can dưới Sàn 1
Place-Elem '4' $x0 0 2730 270 1200 | Out-Null # Lan can trên Sàn 1
Place-Elem '4' $x0 0 4080 270 1200 | Out-Null # Lan can dưới Sàn 2
Place-Elem '4' $x0 0 4530 270 1200 | Out-Null # Lan can trên Sàn 2
Place-Elem '7' $x0 0 1830 270 1200 | Out-Null # Ván chắn đầu hồi Sàn 1
Place-Elem '7' $x0 0 3630 270 1200 | Out-Null # Ván chắn đầu hồi Sàn 2
$count += 6

# 5. Đóng đầu hồi ngoài cùng tại X = 7800 (Nhịp 5)
$xEnd = $xLines[5]
Place-Elem '4' $xEnd 0 2280 270 1200 | Out-Null # Lan can dưới Sàn 1
Place-Elem '4' $xEnd 0 2730 270 1200 | Out-Null # Lan can trên Sàn 1
Place-Elem '4' $xEnd 0 4080 270 1200 | Out-Null # Lan can dưới Sàn 2
Place-Elem '4' $xEnd 0 4530 270 1200 | Out-Null # Lan can trên Sàn 2
Place-Elem '7' $xEnd 0 1830 270 1200 | Out-Null # Ván chắn đầu hồi Sàn 1
Place-Elem '7' $xEnd 0 3630 270 1200 | Out-Null # Ván chắn đầu hồi Sàn 2
$count += 6

# 6. Dọc theo 5 nhịp chính (1800 - 1200 - 1800 - 1200 - 1800)
for ($i = 0; $i -lt 5; $i++) {
    $xSt = $xLines[$i]
    $len = $bayLens[$i]

    # Giằng chân dọc (Z = 235 mm): trước & sau
    Place-Elem '4' $xSt 0 235 0 $len | Out-Null
    Place-Elem '4' $xSt -1200 235 0 $len | Out-Null
    $count += 2

    # Giằng giữa h3 (Z = 930 mm): mặt trước Y = 0
    Place-Elem '4' $xSt 0 930 0 $len | Out-Null
    $count += 1

    # Lan can an toàn mặt trước Y = 0: Sàn 1 (2280, 2730) & Sàn 2 (4080, 4530)
    Place-Elem '4' $xSt 0 2280 0 $len | Out-Null
    Place-Elem '4' $xSt 0 2730 0 $len | Out-Null
    Place-Elem '4' $xSt 0 4080 0 $len | Out-Null
    Place-Elem '4' $xSt 0 4530 0 $len | Out-Null
    $count += 4

    # Giằng đỉnh (Z = 6330 mm): trước & sau
    Place-Elem '4' $xSt 0 6330 0 $len | Out-Null
    Place-Elem '4' $xSt -1200 6330 0 $len | Out-Null
    $count += 2

    # Mâm sàn thao tác Family 6 (W = 500 mm): Sàn 1 & Sàn 2 tại Y = -345 và Y = -855
    Place-Elem '6' $xSt -345 1830 0 $len 500 | Out-Null
    Place-Elem '6' $xSt -855 1830 0 $len 500 | Out-Null
    Place-Elem '6' $xSt -345 3630 0 $len 500 | Out-Null
    Place-Elem '6' $xSt -855 3630 0 $len 500 | Out-Null
    $count += 4

    # Ván chắn biên Family 7: Sàn 1 & Sàn 2 tại Y = -66 và Y = -1134
    Place-Elem '7' $xSt -66 1830 0 $len | Out-Null
    Place-Elem '7' $xSt -1134 1830 0 $len | Out-Null
    Place-Elem '7' $xSt -66 3630 0 $len | Out-Null
    Place-Elem '7' $xSt -1134 3630 0 $len | Out-Null
    $count += 4

    # Giằng chéo Family 5 (mặt sau Y = -1200): Tầng 1 (480 mm), Sàn 1 (1830 mm), Sàn 2 (3630 mm)
    Place-Elem '5' $xSt -1200 480 0 $len 0 900 | Out-Null
    Place-Elem '5' $xSt -1200 1830 0 $len 0 900 | Out-Null
    Place-Elem '5' $xSt -1200 3630 0 $len 0 900 | Out-Null
    $count += 3
}

$sw.Stop()
Write-Host "============================================================"
Write-Host "HOÀN TẤT DỰNG TRƯỜNG HỢP 1.0 QUA MCP!"
Write-Host "Tổng số cấu kiện: $count"
Write-Host "Thời gian thực thi: $($sw.ElapsedMilliseconds) ms ($([math]::Round($sw.Elapsed.TotalSeconds, 2)) s)"
Write-Host "============================================================"
