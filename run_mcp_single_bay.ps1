# ==============================================================================
# SCRIPT DỰNG 1 NHỊP TIÊU CHUẨN (1800 x 1200 MM) - 100% THEO BƯỚC 1-11 CỦA 1800x1200.MD
# THỰC THI QUA MCP BRIDGE (PORT 4848) - MỤC TIÊU < 5 GIÂY
# ==============================================================================
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$baseUrl = "http://127.0.0.1:4848"

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

Write-Host "=== BẮT ĐẦU DỰNG 1 NHỊP TIÊU CHUẨN 1800x1200 QUA MCP ==="
$count = 0

# BƯỚC 1: Đặt Ván Lót Đáy (Family 1) tại Z = 0
Place-Elem '1' 0 0 0 0 1800 | Out-Null
Place-Elem '1' 0 -1200 0 0 1800 | Out-Null
$count += 2
Write-Host "-> Bước 1: Đã đặt 2 ván lót đáy Family 1 (L = 1800 mm)"

# BƯỚC 2: Đặt 4 Kích Chân (Family 2, h = 250 mm)
Place-Elem '2' 0 0 0 0 0 0 250 | Out-Null
Place-Elem '2' 1800 0 0 0 0 0 250 | Out-Null
Place-Elem '2' 0 -1200 0 0 0 0 250 | Out-Null
Place-Elem '2' 1800 -1200 0 0 0 0 250 | Out-Null
$count += 4
Write-Host "-> Bước 2: Đã đặt 4 kích chân Family 2 (h = 250 mm)"

# BƯỚC 3: Đặt 4 Cột Chống (Family 3 - 支柱2700 ALINCO) tại Z = 250 mm
$c1 = Place-Elem '3' 0 0 250 0
Set-ColParams $c1 $p2700
$c2 = Place-Elem '3' 1800 0 250 0
Set-ColParams $c2 $p2700
$c3 = Place-Elem '3' 0 -1200 250 0
Set-ColParams $c3 $p2700
$c4 = Place-Elem '3' 1800 -1200 250 0
Set-ColParams $c4 $p2700
$count += 4
Write-Host "-> Bước 3: Đã đặt 4 cột chống 2700 Family 3 (Z = 250 mm)"

# BƯỚC 4: Đặt 4 Thanh Giằng Chân (Family 4) tại h1 (Z = 385 mm)
Place-Elem '4' 0 0 385 0 1800 | Out-Null       # Dọc trước
Place-Elem '4' 0 -1200 385 0 1800 | Out-Null   # Dọc sau
Place-Elem '4' 0 0 385 270 1200 | Out-Null     # Ngang trái
Place-Elem '4' 1800 0 385 270 1200 | Out-Null  # Ngang phải
$count += 4
Write-Host "-> Bước 4: Đã đặt 4 giằng chân Family 4 (Z = 385 mm)"

# BƯỚC 5: Đặt Giằng Dọc Trước (Family 4) tại h3 (Z = 1080 mm)
Place-Elem '4' 0 0 1080 0 1800 | Out-Null
$count += 1
Write-Host "-> Bước 5: Đã đặt 1 giằng dọc trước Family 4 (Z = 1080 mm)"

# BƯỚC 6: Đặt Giằng Chéo (Family 5) tại h2 (Z = 630 mm, mặt sau Y = -1200)
Place-Elem '5' 0 -1200 630 0 1800 0 900 | Out-Null
$count += 1
Write-Host "-> Bước 6: Đã đặt 1 giằng chéo tầng 1 Family 5 (Z = 630 mm)"

# BƯỚC 7: Đặt 2 Thanh Giằng Ngang Đỡ Mâm (Family 4) tại h5 (Z = 1980 mm)
Place-Elem '4' 0 0 1980 270 1200 | Out-Null
Place-Elem '4' 1800 0 1980 270 1200 | Out-Null
$count += 2
Write-Host "-> Bước 7: Đã đặt 2 giằng ngang đỡ mâm Family 4 (Z = 1980 mm)"

# BƯỚC 8: Đặt Giằng Chéo (Family 5) tại h5 (Z = 1980 mm, mặt sau Y = -1200)
Place-Elem '5' 0 -1200 1980 0 1800 0 900 | Out-Null
$count += 1
Write-Host "-> Bước 8: Đã đặt 1 giằng chéo tầng 2 Family 5 (Z = 1980 mm)"

# BƯỚC 9: Đặt 2 Thanh Giằng Dọc Trước (Family 4) tại h6 và h7 (Lan can: Z = 2430 & 2880 mm)
Place-Elem '4' 0 0 2430 0 1800 | Out-Null
Place-Elem '4' 0 0 2880 0 1800 | Out-Null
$count += 2
Write-Host "-> Bước 9: Đã đặt 2 lan can dọc trước Family 4 (Z = 2430, 2880 mm)"

# BƯỚC 10: Đặt 2 Mâm Đi Lại (Family 6, W = 500 mm) tại h5 (Z = 1980 mm)
Place-Elem '6' 0 -345 1980 0 1800 500 | Out-Null
Place-Elem '6' 0 -855 1980 0 1800 500 | Out-Null
$count += 2
Write-Host "-> Bước 10: Đã đặt 2 mâm thao tác Family 6 (Z = 1980 mm)"

# BƯỚC 11: Đặt 2 Ván Chắn Biên (Family 7) tại h5 (Z = 1980 mm)
Place-Elem '7' 0 -66 1980 0 1800 | Out-Null
Place-Elem '7' 0 -1134 1980 0 1800 | Out-Null
$count += 2
Write-Host "-> Bước 11: Đã đặt 2 ván chắn biên Family 7 (Z = 1980 mm)"

$sw.Stop()
Write-Host "============================================================"
Write-Host "HOÀN TẤT DỰNG 1 NHỊP TIÊU CHUẨN 1800x1200 QUA MCP!"
Write-Host "Tổng số cấu kiện: $count"
Write-Host "Thời gian thực thi: $($sw.ElapsedMilliseconds) ms ($([math]::Round($sw.Elapsed.TotalSeconds, 2)) s)"
Write-Host "============================================================"
