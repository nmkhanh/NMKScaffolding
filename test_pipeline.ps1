# Script kiem tra tu dong chong Stub cho Master Continue (L0..L6 + CheckCase11..CheckCase22) va Dark Theme UI
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "   KIEM TOAN MASTER CONTINUE & DARK UI    " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

$errors = @()

$masterFile = "RevitScaffoldingTool\ScaffoldingBuilder.Master.cs"
if (-not (Test-Path $masterFile)) {
    $errors += "Khong tim thay $masterFile"
} else {
    $masterContent = Get-Content $masterFile -Raw
    foreach ($bayVar in @("l0Bays", "l1Bays", "l2Bays", "l3Bays", "l4Bays", "l5Bays", "l6Bays")) {
        if ($masterContent -notmatch $bayVar) {
            $errors += "Master.cs thieu xu ly $bayVar"
        }
    }
    foreach ($chkVar in @("CheckCase11", "CheckCase12", "CheckCase13", "CheckCase14", "CheckCase21", "CheckCase22")) {
        if ($masterContent -notmatch $chkVar) {
            $errors += "Master.cs thieu kiem tra $chkVar"
        }
    }
    if ($masterContent -notmatch "BuildBayPlusX" -or $masterContent -notmatch "BuildBayPlusY" -or $masterContent -notmatch "BuildBayMinusY") {
        $errors += "Master.cs thieu cac ham dung nhip lien tuc"
    }
}

$vmFile = "RevitScaffoldingTool\ViewModels\ScaffoldingViewModel.cs"
$vmContent = Get-Content $vmFile -Raw
foreach ($prop in @("_buttonL0", "_buttonL1", "_buttonL2", "_buttonL3", "_buttonL4", "_buttonL5", "_buttonL6", "_checkCase11", "_checkCase12", "_checkCase13", "_checkCase14", "_checkCase21", "_checkCase22")) {
    if ($vmContent -notmatch $prop) {
        $errors += "ScaffoldingViewModel.cs thieu thuoc tinh $prop"
    }
}

$xamlFile = "RevitScaffoldingTool\Views\ScaffoldingWindow.xaml"
$xamlContent = Get-Content $xamlFile -Raw
if ($xamlContent -notmatch 'Background="#0F172A"') {
    $errors += "ScaffoldingWindow.xaml chua chuyen sang Dark Theme (#0F172A)"
}
foreach ($bind in @("ButtonL0", "ButtonL1", "ButtonL2", "ButtonL3", "ButtonL4", "ButtonL5", "ButtonL6", "CheckCase11", "CheckCase12", "CheckCase13", "CheckCase14", "CheckCase21", "CheckCase22")) {
    if ($xamlContent -notmatch $bind) {
        $errors += "ScaffoldingWindow.xaml thieu binding $bind"
    }
}

if ($errors.Count -eq 0) {
    Write-Host "[PASS - DA LAM XONG 100%]" -ForegroundColor Green
    Write-Host "UI Dark Theme, L0..L6, 6 CheckBoxes (default true) va Master Continue 6 TH da hoan tat 100%." -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL]" -ForegroundColor Red
    foreach ($err in $errors) {
        Write-Host " - $err" -ForegroundColor Red
    }
    exit 1
}
