$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$revitDir = "C:\Program Files\Autodesk\Revit 2022"
$srcDir = "C:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\RevitScaffoldingTool"
$outDir = "C:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\bin"
$outDll = "$outDir\RevitScaffoldingTool.dll"

if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

$csFiles = (Get-ChildItem -Path $srcDir -Filter "*.cs").FullName

$params = @(
    "/target:library",
    "/out:$outDll",
    "/reference:$revitDir\RevitAPI.dll",
    "/reference:$revitDir\RevitAPIUI.dll",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.dll",
    "/reference:System.Core.dll"
) + $csFiles

& $csc $params
if ($LASTEXITCODE -eq 0) {
    Write-Host "SUCCESS: $outDll"
} else {
    Write-Host "FAILED: $LASTEXITCODE"
}
