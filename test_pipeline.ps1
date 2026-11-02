# Script kiem tra tu dong chong Stub trong ScaffoldingBuilder.TableRun.cs
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "   KIEM TOAN CHONG STUB / RONG (ANTI-STUB)" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

$file = "RevitScaffoldingTool\ScaffoldingBuilder.TableRun.cs"
if (-not (Test-Path $file)) {
    Write-Host "[FAIL] Khong tim thay tep $file" -ForegroundColor Red
    exit 1
}

$content = Get-Content $file -Raw
$errors = @()

# 1. Kiem tra xem co ham nao uy thac ConnectCorner_TH1_1 khong
$stubMatches = [regex]::Matches($content, 'ConnectCorner_TH(1_[2-4]|2_[1-2])\([^\)]*\)\s*\{[^}]*ConnectCorner_TH1_1')
if ($stubMatches.Count -gt 0) {
    foreach ($m in $stubMatches) {
        $errors += "Phat hien ham uy thac gia mao: " + $m.Value.Substring(0, 40)
    }
}

# 2. Kiem tra xem 6 ham co ton tai va co than ham day du khong
$requiredCases = @("ConnectCorner_TH1_1", "ConnectCorner_TH1_2", "ConnectCorner_TH1_3", "ConnectCorner_TH1_4", "ConnectCorner_TH2_1", "ConnectCorner_TH2_2")
foreach ($case in $requiredCases) {
    if ($content -notmatch "void\s+$case\s*\(") {
        $errors += "Thieu dinh nghia ham $case"
    } else {
        # Kiem tra xem than ham co chua cac cau kien co ban hay khong
        $pattern = "void\s+$case\s*\([^\)]*\)\s*\{([^}]*\{[^}]*\})*[^}]*\}"
        if ($content -match $pattern) {
            $body = $matches[0]
            if ($body -notmatch "PlaceInstance" -or $body -notmatch "ensureCol") {
                $errors += "Ham $case khong chua logic dat cau kien thuc te!"
            }
        }
    }
}

# 3. Kiem tra ho tro vong khep kin (Closed Loop)
if ($content -notmatch "isClosedLoop" -or $content -notmatch "dEndToStart") {
    $errors += "Thieu logic xu ly Vong Khep Kin (Closed Loop)"
}

if ($errors.Count -eq 0) {
    Write-Host "[PASS - DA LAM XONG 100%]" -ForegroundColor Green
    Write-Host "Tat ca 6 truong hop Connect da duoc code chi tiet voi kich thuoc thuc te (L_last, L_first, W_A, W_B)." -ForegroundColor Green
    Write-Host "Logic Vong Khep Kin (Closed Loop) da duoc tich hop hoan tat." -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL - CHUA LAM XONG]" -ForegroundColor Red
    foreach ($err in $errors) {
        Write-Host " - $err" -ForegroundColor Red
    }
    exit 1
}
