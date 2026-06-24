#!/usr/bin/env pwsh
param(
    [Parameter(Mandatory = $false)]
    [string] $SpecUrl = "https://localhost:7030/openapi/v1.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot/../.."
Set-Location $repoRoot

Write-Host "Running NSwag generation against $SpecUrl..."
./scripts/openapi/generate-api-clients.ps1 -SpecUrl $SpecUrl

$targetFile = "src/Host/AMIS.Blazor/ApiClient/Generated.cs"

Write-Host "Checking for drift in $targetFile..."
git diff --exit-code -- $targetFile

Write-Host "No drift detected."
