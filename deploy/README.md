# AMIS Deployment — Database Migration Runbook

This folder holds **operations tooling**, not application code. Nothing here runs as part of the
app, the build, or local development. It executes only when you run it by hand at deploy time.

## Why a per-module runbook?

AMIS is a **modular monolith** with **one EF Core `DbContext` per module**. EF migrations are scoped
to a single `DbContext`, so a database is stood up by running `dotnet ef database update` **once per
context** — 11 times total. Miss one and that module's tables won't exist; the app still boots, then
throws `relation "..." does not exist` the moment a user opens that feature.

`appsettings.Production.json` has `MultitenancyOptions.RunTenantMigrationsOnStartup = false`, so the
schema is **not** auto-created on boot — applying it is a deliberate deploy step. That's this script.

## The 11 contexts (in apply order)

| # | Module | DbContext |
|---|--------|-----------|
| 1 | Identity | `IdentityDbContext` |
| 2 | Multitenancy | `TenantDbContext` |
| 3 | Auditing | `AuditDbContext` |
| 4 | MasterData | `MasterDataDbContext` |
| 5 | Expendable | `ExpendableDbContext` |
| 6 | AssetManagement | `AssetManagementDbContext` |
| 7 | AssetRegister | `AssetRegisterDbContext` |
| 8 | Vehicle | `VehicleDbContext` |
| 9 | Finance | `FinanceDbContext` |
| 10 | ProcurementPlanning | `ProcurementPlanningDbContext` |
| 11 | ProcurementAcquisition | `ProcurementDbContext` |

Identity + Multitenancy go first (auth and tenant resolution underpin everything); Auditing next; the
business modules after. Order among the business modules is not strict but is kept stable.

## Prerequisites

- **.NET 10 SDK** on the machine running the script.
- **`dotnet-ef` tool**, version matching the runtime (10.0.7+):
  ```powershell
  dotnet tool update --global dotnet-ef
  ```
- Network access from this machine to the target PostgreSQL server.
- A DB login with rights to create schemas/tables on the target database.

## Usage

Run from the **repo root**.

### Dry run first (touches nothing)

```powershell
./deploy/migrate-postgres.ps1 `
    -ConnectionString "Host=10.0.0.5;Port=5432;Database=AMIS;Username=AMIS_app;Password=secret" `
    -WhatIf
```

### Apply for real

```powershell
./deploy/migrate-postgres.ps1 `
    -ConnectionString "Host=10.0.0.5;Port=5432;Database=AMIS;Username=AMIS_app;Password=secret"
```

The script builds once, then applies each context with `--no-build`. It **stops on the first
failure** so you never get a silently half-migrated database. Re-running is safe: contexts already
at their latest migration are skipped (idempotent).

### Flags

| Flag | Effect |
|------|--------|
| `-ConnectionString` | **(required)** Npgsql connection string for the target DB. Password is masked in all console output. |
| `-WhatIf` | Print the planned commands without executing — no DB changes, no build. |
| `-NoBuild` | Skip the upfront build (use only if you just built the solution). |

## What this script does NOT do

- It does **not** generate migrations or modify any `.cs` / snapshot files.
- It does **not** touch application code or the build pipeline.
- It does **not** affect any database except the one in `-ConnectionString`.

It only **applies migrations that already exist** under `src/Playground/Migrations.PostgreSQL/`.

## Running a single context manually

If you ever need just one module (e.g. after adding a migration to AssetRegister):

```powershell
dotnet ef database update `
    --context AssetRegisterDbContext `
    --project src/Playground/Migrations.PostgreSQL `
    --startup-project src/Playground/Playground.Api `
    --connection "Host=10.0.0.5;Database=AMIS;Username=AMIS_app;Password=secret"
```
