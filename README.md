# AMIS102

AMIS102 is a modular, enterprise-ready Asset Management Information System built on .NET 10 and powered by the AMIS (Asset Management Information System) (AMIS) architecture foundation.

This repository extends the AMIS framework into a domain-focused implementation for government and enterprise operations, including procurement, fixed assets, expendables, vehicle management, finance, auditing, and multi-tenant administration.

## What this project delivers

- Modular monolith architecture with clear bounded contexts and vertical slices.
- CQRS + DDD implementation using Minimal APIs, Mediator, FluentValidation, and EF Core.
- Multi-tenant support from day one using Finbuckle.MultiTenant.
- Built-in Identity, authorization permissions, auditing, caching, jobs, and OpenAPI docs.
- Reference clients for API, Blazor, and .NET MAUI.

## Core modules in AMIS102

- Identity
- Multitenancy
- Auditing
- MasterData
- Expendable
- AssetManagement
- AssetProcurement
- Vehicle
- Finance
- ProcurementPlanning
- ProcurementAcquisition

## Technology stack

- .NET 10, C# latest, Minimal APIs
- Mediator (source-generated), FluentValidation
- EF Core 10 with PostgreSQL (SQL Server-ready)
- ASP.NET Identity + JWT auth
- Redis distributed caching
- Hangfire background jobs
- OpenTelemetry + health checks + structured logging

## Repository structure

- `src/BuildingBlocks` - Reusable framework packages (core, persistence, web, caching, jobs, mailing, etc.)
- `src/Modules` - Business modules and feature slices
- `src/Playground` - Executable hosts and clients
- `src/Tests` - Architecture and module guardrail tests
- `scripts` - Automation scripts (including OpenAPI client generation)
- `terraform` - Infrastructure scaffolding

## Prerequisites

- .NET 10 SDK
- .NET Aspire workload (for AppHost mode)
- Docker Desktop (for local Postgres and Redis when running Aspire)

## Quick start

1. Restore dependencies:
   `dotnet restore src/AMIS.Framework.slnx`
2. Build solution:
   `dotnet build src/AMIS.Framework.slnx`
3. Run full stack with Aspire:
   `dotnet run --project src/Playground/AMIS.Playground.AppHost`

Aspire will orchestrate the API, supporting services, and local infrastructure.

## Alternative run modes

- API only:
  `dotnet run --project src/Playground/Playground.Api`
- Build:
  `dotnet build src/AMIS.Framework.slnx`
- Test:
  `dotnet test src/AMIS.Framework.slnx`

## Development conventions

- Use `ICommand<T>` and `IQuery<T>` (not MediatR `IRequest<T>`).
- Handlers return `ValueTask<T>`.
- Every command/query includes validation.
- Endpoints enforce permissions explicitly.
- Keep build output warning-free.

## Notes on project identity

This repository is AMIS102, implemented on top of the AMIS (Asset Management Information System) starter architecture. If you see references to AMIS in solution or framework package names, those refer to the underlying platform components used by AMIS102.

## Additional docs

- `CLAUDE.md` for implementation rules and coding conventions
- `MAUI-IMPLEMENTATION-PLAN.md` for .NET MAUI roadmap
- `ASSETMANAGEMENT-OVERHAUL-PLAN.md` and related planning docs for domain evolution

AMIS102 is designed for production-focused teams that need fast delivery with strong architecture discipline.

