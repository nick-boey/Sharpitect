#!/usr/bin/env pwsh
# Packs and installs Sharpitect as a local .NET tool

$ErrorActionPreference = "Stop"

$scriptDir = $PSScriptRoot
$nupkgDir = Join-Path $scriptDir "nupkg"
$cliProject = Join-Path $scriptDir "src/Sharpitect.CLI/Sharpitect.CLI.csproj"

dotnet tool uninstall Sharpitect --global

Write-Host "Packing Sharpitect.CLI..." -ForegroundColor Cyan
dotnet pack $cliProject -c Release -o $nupkgDir

if ($LASTEXITCODE -ne 0)
{
    Write-Host "Pack failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`nInstalling Sharpitect tool..." -ForegroundColor Cyan

if ($LASTEXITCODE -ne 0)
{
    Write-Host "Tool not found, installing fresh..." -ForegroundColor Yellow
    dotnet tool install --global --add-source $nupkgDir Sharpitect
}

if ($LASTEXITCODE -eq 0)
{
    Write-Host "`nSharpitect installed successfully!" -ForegroundColor Green
    Write-Host "Run 'sharpitect --help' to get started."
}
else
{
    Write-Host "`nInstallation failed!" -ForegroundColor Red
    exit 1
}
