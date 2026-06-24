#requires -Version 5.1
<#
.SYNOPSIS
    Applies all AMIS module EF Core migrations to a target PostgreSQL database, in dependency order.

.DESCRIPTION
    AMIS is a modular monolith with ONE DbContext per module. EF Core migrations are scoped to a
    single DbContext, so standing up a database requires running `dotnet ef database update` once
    per context. This script runs all 11 contexts in the correct order and stops on the first failure.

    It only ever touches the database named in the connection string you pass. It does NOT generate
    migrations, modify source, or change snapshots - it only APPLIES migrations that already exist
    in src/Host/Migrations.PostgreSQL.

.PARAMETER ConnectionString
    Full Npgsql connection string for the target database, e.g.
    "Host=pg-host;Port=5432;Database=AMIS;Username=AMIS_app;Password=secret"

.PARAMETER NoBuild
    Skip building the migrations/startup projects (use only if you've just built them).

.PARAMETER WhatIf
    Print the commands that would run without executing them.

.EXAMPLE
    ./deploy/migrate-postgres.ps1 -ConnectionString "Host=10.0.0.5;Database=AMIS;Username=AMIS_app;Password=***"

.EXAMPLE
    # Dry run - see the plan without touching any database
    ./deploy/migrate-postgres.ps1 -ConnectionString "Host=x;Database=AMIS;Username=u;Password=p" -WhatIf
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString,

    [switch]$NoBuild,

    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'

# Resolve repo paths relative to this script (deploy/ -> repo root).
$repoRoot       = Split-Path -Parent $PSScriptRoot
$migrationsProj = Join-Path $repoRoot 'src/Host/Migrations.PostgreSQL'
$startupProj    = Join-Path $repoRoot 'src/Host/AMIS.Api'

# -----------------------------------------------------------------------------
# Ordered list of module DbContexts. Order matters:
#   1. Identity + Multitenancy first - auth and tenant resolution underpin everything.
#   2. Auditing next - cross-cutting, depended on by business modules.
#   3. Business modules last - order among them is not strict, but kept stable here.
# Each entry maps a friendly module name to its EF DbContext type name.
# -----------------------------------------------------------------------------
$contexts = [ordered]@{
    'Identity'               = 'IdentityDbContext'
    'Multitenancy'           = 'TenantDbContext'
    'Auditing'               = 'AuditDbContext'
    'MasterData'             = 'MasterDataDbContext'
    'Expendable'             = 'ExpendableDbContext'
    'AssetManagement'        = 'AssetManagementDbContext'
    'AssetRegister'          = 'AssetRegisterDbContext'
    'Vehicle'                = 'VehicleDbContext'
    'Finance'                = 'FinanceDbContext'
    'ProcurementPlanning'    = 'ProcurementPlanningDbContext'
    'ProcurementAcquisition' = 'ProcurementDbContext'
}

Write-Host "AMIS PostgreSQL migration runbook" -ForegroundColor Cyan
Write-Host "  Migrations project : $migrationsProj"
Write-Host "  Startup project    : $startupProj"
# Mask the password when echoing the target.
$maskedConn = $ConnectionString -replace '(?i)(Password|Pwd)\s*=\s*[^;]*', '$1=****'
Write-Host "  Target             : $maskedConn"
Write-Host "  Contexts           : $($contexts.Count)"
Write-Host ""

# Build once up front so each `database update` can use --no-build (faster, and avoids
# rebuilding 11 times). Skip if -NoBuild was passed.
if (-not $NoBuild -and -not $WhatIf) {
    Write-Host "Building migrations + startup projects..." -ForegroundColor Yellow
    dotnet build $startupProj -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw "Build failed - aborting before any migration ran." }
    Write-Host ""
}

$step = 0
foreach ($module in $contexts.Keys) {
    $step++
    $ctx = $contexts[$module]
    Write-Host "[$step/$($contexts.Count)] $module ($ctx)" -ForegroundColor Green

    $efArgs = @(
        'ef', 'database', 'update',
        '--context', $ctx,
        '--project', $migrationsProj,
        '--startup-project', $startupProj,
        '--connection', $ConnectionString
    )
    if (-not $NoBuild) { $efArgs += '--no-build' }

    if ($WhatIf) {
        Write-Host "    WHATIF > dotnet $($efArgs -replace [regex]::Escape($ConnectionString), $maskedConn)" -ForegroundColor DarkGray
        continue
    }

    & dotnet @efArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Migration FAILED for $module ($ctx). Database may be partially migrated. Fix the cause and re-run - already-applied contexts are idempotent and will be skipped."
    }
    Write-Host ""
}

if ($WhatIf) {
    Write-Host "Dry run complete - no database was modified." -ForegroundColor Cyan
} else {
    Write-Host "All $($contexts.Count) module contexts migrated successfully." -ForegroundColor Cyan
}
