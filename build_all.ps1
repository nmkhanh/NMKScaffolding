$ErrorActionPreference = "Stop"

$workspaceRoot = $PSScriptRoot
$scaffoldingCsproj = Join-Path $workspaceRoot "RevitScaffoldingTool\RevitScaffoldingTool.csproj"
$mcpCsproj = Join-Path $workspaceRoot "MCPRevit\RevitMcpAddin\RevitMcpAddin.csproj"

$versions = @("R2022", "R2024", "R2026")
$results = @()

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   BUILD ALL VERSIONS (Revit 2022 / 2024 / 2026)          " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Build RevitScaffoldingTool (WPF UI Add-in)
foreach ($ver in $versions) {
    Write-Host "`n[BUILDING] RevitScaffoldingTool - Configuration: $ver..." -ForegroundColor Yellow
    $cfg = if ($ver -eq "R2022") { "Release" } else { $ver }
    & dotnet build "$scaffoldingCsproj" -c $cfg -v q --nologo
    $status = if ($LASTEXITCODE -eq 0) { "SUCCESS" } else { "FAILED ($LASTEXITCODE)" }
    $results += [PSCustomObject]@{
        Project = "RevitScaffoldingTool"
        RevitVersion = $ver
        Config = $cfg
        Status = $status
    }
}

# 2. Build RevitMcpAddin (MCP Bridge Add-in)
foreach ($ver in $versions) {
    Write-Host "`n[BUILDING] RevitMcpAddin - Configuration: $ver..." -ForegroundColor Yellow
    & dotnet build "$mcpCsproj" -c $ver -p:SkipRevitDeploy=true -v q --nologo
    $status = if ($LASTEXITCODE -eq 0) { "SUCCESS" } else { "FAILED ($LASTEXITCODE)" }
    $results += [PSCustomObject]@{
        Project = "RevitMcpAddin"
        RevitVersion = $ver
        Config = $ver
        Status = $status
    }
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "   BUILD SUMMARY TABLE                                    " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
$results | Format-Table -AutoSize
