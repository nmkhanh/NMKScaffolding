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

Write-Host "=== 1. CHUYỂN CẤU KIỆN LỖI 13865, 13866 RA KHỎI PHẠM VI VIEW ==="
Post-Json "$baseUrl/set_parameter" @{elementId=13865; paramName='Offset from Host'; value=-100000} | Out-Null
Post-Json "$baseUrl/set_parameter" @{elementId=13866; paramName='Offset from Host'; value=-100000} | Out-Null

Write-Host "=== 2. ĐẶT LẠI 2 VÁN LÓT ĐÁY FAMILY 1 ĐÚNG TỌA ĐỘ VÀ ROT=90 ==="
Place-Elem '1' 36600 -3000 0 90 1800 | Out-Null
Place-Elem '1' 37800 -3000 0 90 1800 | Out-Null

Write-Host "=== 3. ĐẶT GIẰNG CHÉO FAMILY 5 CHO NHÁNH -Y TẠI X = 37800 ==="
# Nhánh -Y tại mặt ngoài X = 37800: từ Y = -3000 đến -1200 (L = 1800 mm, rot = 90)
Place-Elem '5' 37800 -3000 480 90 1800 0 900 | Out-Null
Place-Elem '5' 37800 -3000 1830 90 1800 0 900 | Out-Null
Place-Elem '5' 37800 -3000 3630 90 1800 0 900 | Out-Null

Write-Host "=== 4. ĐẶT GIẰNG CHÉO FAMILY 5 CHO 5 NHỊP DÃY CHÍNH TẠI Y = 0 ==="
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

Write-Host "=== HOÀN TẤT BỔ SUNG CẤU KIỆN CHO MODEL 2 (-Y BRANCH) ==="
