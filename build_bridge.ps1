$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$revitDir = "C:\Program Files\Autodesk\Revit 2022"
$srcDir = "C:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\revit_2022_mcp_bridge"
$outDir = "C:\ProgramData\Autodesk\Revit\Addins\2022\RevitFamilyMcpBridge"
$outDll = "$outDir\RevitFamilyMcpBridge.dll"

if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force
}

$csFiles = Get-ChildItem -Path $srcDir -Filter "*.cs" | ForEach-Object { $_.FullName }

$args = @(
    "/target:library",
    "/out:$outDll",
    "/reference:$revitDir\RevitAPI.dll",
    "/reference:$revitDir\RevitAPIUI.dll",
    "/reference:System.Web.Extensions.dll"
) + $csFiles

& $csc $args
if ($LASTEXITCODE -eq 0) {
    Write-Host "COMPILED SUCCESSFULLY: $outDll"
} else {
    Write-Host "COMPILE FAILED with code $LASTEXITCODE"
}
