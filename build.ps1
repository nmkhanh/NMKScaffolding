$ErrorActionPreference = 'Stop'
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outDir = 'c:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\bin'
if (!(Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

$revitDir = 'C:\Program Files\Autodesk\Revit 2022'
$target = "$outDir\RevitScaffoldingTool.dll"
$srcDir = 'c:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\RevitScaffoldingTool'

# Create a response file for csc to completely avoid command line escaping issues
$rspFile = "$outDir\build.rsp"
$rspContent = @(
    "/target:library",
    "/platform:x64",
    "/optimize+",
    "/out:`"$target`"",
    "/reference:`"$revitDir\RevitAPI.dll`"",
    "/reference:`"$revitDir\RevitAPIUI.dll`"",
    "/reference:System.dll",
    "/reference:System.Core.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Windows.Forms.dll"
)

$csFiles = Get-ChildItem -Path $srcDir -Filter '*.cs' | ForEach-Object { "`"$($_.FullName)`"" }
$rspContent += $csFiles

$rspContent | Set-Content -Path $rspFile -Encoding UTF8

Write-Host "Compiling RevitScaffoldingTool.dll via response file..."
$process = Start-Process -FilePath $csc -ArgumentList "@`"$rspFile`"" -NoNewWindow -Wait -PassThru

if ($process.ExitCode -eq 0) {
    Write-Host "BUILD SUCCESSFUL!" -ForegroundColor Green
    Get-Item $target | Select-Object Name, Length, LastWriteTime
} else {
    Write-Host "BUILD FAILED with exit code $($process.ExitCode)" -ForegroundColor Red
}
