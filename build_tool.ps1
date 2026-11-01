$ErrorActionPreference = "Stop"
$project = "C:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\RevitScaffoldingTool\RevitScaffoldingTool.csproj"
$outDir = "C:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\bin"
$outDll = "$outDir\RevitScaffoldingTool.dll"

Write-Host "Building RevitScaffoldingTool (WPF / MVVM) with dotnet build..." -ForegroundColor Cyan
& dotnet build $project -c Release -v minimal

if ($LASTEXITCODE -eq 0) {
    Write-Host "SUCCESS: $outDll" -ForegroundColor Green
} else {
    Write-Host "FAILED: $LASTEXITCODE" -ForegroundColor Red
}
