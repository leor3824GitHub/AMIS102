#!/usr/bin/env pwsh
# Test CLI locally without publishing to NuGet
# Usage: ./scripts/test-cli.ps1 [-Version "10.0.0-rc.1"] [-Uninstall]

param(
    [string]$Version = "10.0.0-local",
    [switch]$Uninstall,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$ScriptDir = $PSScriptRoot
if (-not $ScriptDir) { $ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path }
$RepoRoot = Split-Path -Parent $ScriptDir
$CliProject = Join-Path $RepoRoot "src\Tools\CLI\AMIS.CLI.csproj"
$NupkgsDir = Join-Path $RepoRoot "artifacts\nupkgs"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  AMIS CLI Local Test Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Repo Root: $RepoRoot" -ForegroundColor Gray
Write-Host "CLI Project: $CliProject" -ForegroundColor Gray
Write-Host ""

# Uninstall existing CLI
Write-Host "[1/4] Uninstalling existing AMIS tool..." -ForegroundColor Yellow
dotnet tool uninstall -g AMIS.CLI 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "      Uninstalled successfully" -ForegroundColor Green
} else {
    Write-Host "      Not installed (skipping)" -ForegroundColor Gray
}

if ($Uninstall) {
    Write-Host ""
    Write-Host "Uninstall complete." -ForegroundColor Green
    exit 0
}

# Build and pack
if (-not $SkipBuild) {
    Write-Host ""
    Write-Host "[2/4] Building and packing CLI (Version: $Version)..." -ForegroundColor Yellow

    # Clean artifacts
    if (Test-Path $NupkgsDir) {
        Remove-Item -Recurse -Force $NupkgsDir
    }
    New-Item -ItemType Directory -Force -Path $NupkgsDir | Out-Null

    # Build with version
    dotnet build $CliProject -c Release -p:Version=$Version
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }

    # Pack with version
    dotnet pack $CliProject -c Release --no-build -o $NupkgsDir -p:PackageVersion=$Version
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Pack failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "      Package created successfully" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "[2/4] Skipping build (using existing package)..." -ForegroundColor Gray
}

# Install from local package
Write-Host ""
Write-Host "[3/4] Installing CLI from local package..." -ForegroundColor Yellow
$PackagePath = Get-ChildItem -Path $NupkgsDir -Filter "AMIS.CLI.*.nupkg" | Select-Object -First 1

if (-not $PackagePath) {
    Write-Host "No package found in $NupkgsDir" -ForegroundColor Red
    exit 1
}

Write-Host "      Package: $($PackagePath.Name)" -ForegroundColor Gray
dotnet tool install -g AMIS.CLI --add-source $NupkgsDir --version $Version
if ($LASTEXITCODE -ne 0) {
    Write-Host "Install failed!" -ForegroundColor Red
    exit 1
}
Write-Host "      Installed successfully" -ForegroundColor Green

# Verify installation
Write-Host ""
Write-Host "[4/4] Verifying installation..." -ForegroundColor Yellow
Write-Host ""

$AMISPath = Get-Command AMIS -ErrorAction SilentlyContinue
if ($AMISPath) {
    Write-Host "      AMIS location: $($AMISPath.Source)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "----------------------------------------" -ForegroundColor Cyan
    AMIS --version
    Write-Host "----------------------------------------" -ForegroundColor Cyan
} else {
    Write-Host "      Warning: 'AMIS' command not found in PATH" -ForegroundColor Yellow
    Write-Host "      You may need to restart your terminal" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  CLI installed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Test commands:" -ForegroundColor Cyan
Write-Host "  AMIS --help" -ForegroundColor White
Write-Host "  AMIS new --help" -ForegroundColor White
Write-Host "  AMIS new MyApp" -ForegroundColor White
Write-Host ""
Write-Host "To uninstall:" -ForegroundColor Cyan
Write-Host "  ./scripts/test-cli.ps1 -Uninstall" -ForegroundColor White
Write-Host ""

