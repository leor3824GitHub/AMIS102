# AMIS — Asset Management Information System

AMIS is a modular, enterprise-ready Asset Management Information System built on .NET 10 and powered by the AMIS architecture foundation.

This repository extends the AMIS framework into a domain-focused implementation for government and enterprise operations, including procurement, fixed assets, expendables, vehicle management, finance, auditing, and multi-tenant administration.

---

## What This Project Delivers

- **Modular monolith architecture** with clear bounded contexts and vertical slices
- **CQRS + DDD implementation** using Minimal APIs, Mediator, FluentValidation, and EF Core
- **Multi-tenant support** from day one using Finbuckle.MultiTenant
- **Built-in Identity**, authorization permissions, auditing, caching, jobs, and OpenAPI docs
- **Reference clients** for API, Blazor, and .NET MAUI

---

## Core Modules

| Module | Purpose |
|--------|---------|
| **Identity** | Authentication, users, roles, permissions |
| **Multitenancy** | Tenant management and isolation |
| **Auditing** | Audit logging and change tracking |
| **MasterData** | Reference data (units, categories, employees, etc.) |
| **AssetManagement** | Fixed asset tracking (ICS, PAR, SMIR, PPEIR) |
| **AssetRegister** | Unified asset registry (new) |
| **AssetProcurement** | Asset procurement workflow & IAR |
| **Expendable** | Consumable supplies management |
| **Vehicle** | Fleet and vehicle management |
| **Finance** | Disbursement vouchers, budget utilization |
| **ProcurementPlanning** | Annual procurement planning |
| **ProcurementAcquisition** | Purchase requests, canvass, POs |

---

## Technology Stack

- **.NET 10**, C# latest, **Minimal APIs**
- **Mediator** (source-generated), **FluentValidation**
- **EF Core 10** with **PostgreSQL** (SQL Server-ready)
- **ASP.NET Identity + JWT** auth
- **Redis** distributed caching
- **Hangfire** background jobs
- **OpenTelemetry + health checks + structured logging**

---

## Repository Structure

```
src/
├── BuildingBlocks/      # Reusable framework packages (core, persistence, web, caching, etc.)
├── Modules/             # Business modules and feature slices
│   ├── Identity/
│   ├── AssetManagement/
│   ├── AssetRegister/
│   ├── AssetProcurement/
│   ├── Expendable/
│   ├── ProcurementPlanning/
│   ├── ProcurementAcquisition/
│   └── ...
├── Host/                # Executable hosts and reference clients
│   ├── AMIS.AppHost/    # .NET Aspire orchestration
│   ├── AMIS.Api/              # API host
│   ├── AMIS.Blazor/           # Blazor UI
│   └── AMIS.Maui/             # .NET MAUI mobile/desktop
└── Tests/               # Architecture and module tests
scripts/                # Automation scripts (OpenAPI client generation, etc.)
terraform/              # Infrastructure scaffolding (IaC)
```

---

## Prerequisites

- **.NET 10 SDK**
- **.NET Aspire workload** (for AppHost mode)
- **Docker Desktop** (for local Postgres and Redis)

---

## Quick Start

### 1. Restore & Build
```powershell
dotnet restore src/AMIS.Framework.slnx
dotnet build src/AMIS.Framework.slnx
```

### 2. Run Full Stack with Aspire
```powershell
dotnet run --project src/Host/AMIS.AppHost
```

Aspire will orchestrate the API, supporting services, and local infrastructure.

### 3. Alternative Run Modes
```powershell
# API only
dotnet run --project src/Host/AMIS.Api

# Run tests
dotnet test src/AMIS.Framework.slnx

# Build without running
dotnet build src/AMIS.Framework.slnx
```

---

## Development Conventions

- Use `ICommand<T>` and `IQuery<T>` (not MediatR `IRequest<T>`)
- Handlers return `ValueTask<T>`
- Every command/query includes validation
- Endpoints enforce permissions explicitly
- **Keep build output warning-free** (CI gate)

See [CLAUDE.md](CLAUDE.md) for complete development patterns.

---

## Documentation Index

### 🚀 Getting Started
- **[CLAUDE.md](CLAUDE.md)** — AI assistant guide, development patterns, and conventions

### 📦 Module Documentation

| Module | Guide |
|--------|-------|
| **Asset Management** | [ASSETMANAGEMENT-DOCUMENTATION.md](ASSETMANAGEMENT-DOCUMENTATION.md) — Asset acquisition flow, overhaul plan, report alignment |
| **Asset Register** | [ASSET-REGISTER-DOCUMENTATION.md](ASSET-REGISTER-DOCUMENTATION.md) — New unified asset domain model |
| **IAR Workflow** | [IAR-IMPLEMENTATION-GUIDE.md](IAR-IMPLEMENTATION-GUIDE.md) — 3-stage inspection & acceptance workflow |
| **Expendable** | [EXPENDABLE-DOCUMENTATION.md](EXPENDABLE-DOCUMENTATION.md) — Consumable supplies & shopping carts |
| **Procurement** | [PROCUREMENT-DOCUMENTATION.md](PROCUREMENT-DOCUMENTATION.md) — Planning and acquisition flows |
| **Blazor UI** | [BLAZOR-ARCHITECTURE-GUIDE.md](BLAZOR-ARCHITECTURE-GUIDE.md) — Client generation, API consumption, components |
| **Reporting** | [REPORTING-DOCUMENTATION.md](REPORTING-DOCUMENTATION.md) — FAST and RDLC frameworks |

### 📋 Strategic & Reference Documents
- **[POS-INDUSTRY-STANDARD-ALIGNMENT.md](POS-INDUSTRY-STANDARD-ALIGNMENT.md)** — Point-of-sale system alignment
- **[MUDBLAZOR-SIZING-STANDARDIZATION-PLAN.md](MUDBLAZOR-SIZING-STANDARDIZATION-PLAN.md)** — UI sizing standards
- **[MAUI-IMPLEMENTATION-PLAN.md](MAUI-IMPLEMENTATION-PLAN.md)** — .NET MAUI mobile/desktop roadmap
- **[MCP-POSTGRESQL-USAGE.md](MCP-POSTGRESQL-USAGE.md)** — PostgreSQL MCP server setup
- **[DATA-CONSISTENCY-ANALYSIS.md](DATA-CONSISTENCY-ANALYSIS.md)** — Data consistency strategy
- **[ENTITY-FLOW-AND-MONITORING.md](ENTITY-FLOW-AND-MONITORING.md)** — Entity lifecycle monitoring
- **[MASTERDATA-MODULE-ANALYSIS.md](MASTERDATA-MODULE-ANALYSIS.md)** — Master data architecture
- **[AMIS_AI_Function_Calling_Guide.md](AMIS_AI_Function_Calling_Guide.md)** — LLM integration guide

---

## Project Organization

### Consolidated Documentation

Documentation has been organized into focused guides by functional domain:

| Document | Covers | Replaces |
|----------|--------|----------|
| ASSETMANAGEMENT-DOCUMENTATION.md | Asset acquisition flow, domain overhaul, report alignment | ASSET-ACQUISITION-FLOW-PLAN.md, ASSETMANAGEMENT-OVERHAUL-PLAN.md, ASSETMANAGEMENT-REPORT-ALIGNMENT-CHECKLIST.md |
| ASSET-REGISTER-DOCUMENTATION.md | New unified registry bounded context | ASSET-REGISTER-MODULE-PLAN.md, ASSET-REGISTER-MODULE-PROGRESS.md |
| IAR-IMPLEMENTATION-GUIDE.md | 3-stage workflow design and UI implementation | IAR-WORKFLOW-PLAN.md, IAR-WORKFLOW-PROGRESS.md, IAR-UI-OVERHAUL-PLAN.md |
| EXPENDABLE-DOCUMENTATION.md | Module architecture and integration | EXPENDABLE-MODULE-README.md, EXPENDABLE-QUICKSTART.md, EXPENDABLE-ACQUISITION-FLOW-PLAN.md, EXPENDABLE-INTEGRATION-CHECKLIST.md |
| PROCUREMENT-DOCUMENTATION.md | Planning and acquisition workflows | PROCUREMENTPLANNING-DOMAIN-OVERHAUL-PLAN.md, PROCUREMENTPLANNING-MODULE-FLOW.md, PROCUREMENT_DOMAIN_FIX_PLAN.md |
| BLAZOR-ARCHITECTURE-GUIDE.md | Client generation and API consumption | BLAZOR-API-CLIENT-GENERATION.md, BLAZOR-API-CONNECTION-ARCHITECTURE.md, BLAZOR-API-CONSUMPTION-ALIGNMENT-ANALYSIS.md, BLAZOR-CLIENT-CONFORMANCE-AUDIT.md, BLAZOR-REUSABLE-COMPONENTS-PLAN.md |
| REPORTING-DOCUMENTATION.md | FAST and RDLC reporting frameworks | FAST-REPORTING.md, RDLC-REPORTING.md |

**Original files are archived for reference and cross-linking; consolidated guides are the primary documentation.**

---

## Notes on Project Identity

This repository is AMIS, implemented on top of the AMIS starter architecture. If you see references to AMIS in solution or framework package names, those refer to the underlying platform components used by AMIS.

---

## Build & Test Gates

Before committing, ensure:

```powershell
# Build with zero warnings
dotnet build src/AMIS.Framework.slnx  # ⚠️ No warnings allowed (CI gate)

# All tests pass
dotnet test src/AMIS.Framework.slnx   # ✅ All green

# Code quality
dotnet format --verify-no-changes      # (optional formatting check)
```

---

## Key Features

### 🏛️ Government Compliance
- COA 2020-006 property numbering
- Official form templates (ICS, PAR, SMIR, PPEIR)
- Audit trails and accountability tracking
- Multi-signature approval workflows

### 🔐 Security & Multi-Tenancy
- ASP.NET Identity + JWT authentication
- Role-based access control (RBAC)
- Tenant isolation via Finbuckle.MultiTenant
- Data encryption at rest and in transit

### 📊 Rich Reporting
- FAST Reporting for analytics and dashboards
- RDLC for government compliance forms
- PDF, Excel, Word exports
- Interactive drill-down and filtering

### 📱 Client Variety
- **Blazor Server** for internal staff UI
- **Blazor WebAssembly** for portable web apps
- **.NET MAUI** for mobile/desktop applications
- **OpenAPI** for third-party integrations

### 🔄 Event-Driven Architecture
- Domain events for intra-module coordination
- Integration events for cross-module communication
- Event sourcing for audit trails
- CQRS for scalable queries

---

## Design Principles

1. **Modular Monolith** — Clear bounded contexts, loose coupling, high cohesion
2. **Domain-Driven Design** — Aggregates, value objects, domain services, ubiquitous language
3. **CQRS** — Separate read and write models for performance and clarity
4. **Vertical Slices** — Each feature owns its command, handler, validator, endpoint, and tests
5. **Multi-Tenancy First** — Every entity stamped with TenantId; tenant isolation guaranteed by middleware
6. **Production-Ready** — Health checks, structured logging, OpenTelemetry, graceful degradation

---

## Contributing

### Code Standards
- Follow [CLAUDE.md](CLAUDE.md) conventions
- Every command/query must have a validator
- Tests required for business logic
- Documentation for new modules

### Module Checklist
Before submitting a new module:
- [ ] Domain entities and aggregates defined
- [ ] EF Core migrations included
- [ ] Command/query handlers with validators
- [ ] API endpoints (Minimal APIs)
- [ ] Unit tests for core logic
- [ ] Integration tests for workflows
- [ ] Documentation (README, architecture notes)
- [ ] Build succeeds with 0 warnings
- [ ] All tests pass

---

## Support & Community

- **Issues & Bugs** — GitHub Issues
- **Discussions** — GitHub Discussions
- **Documentation** — See links above

---

## License & Attribution

AMIS is built on the [Full Stack Hero](https://fullstackhero.net/) starter framework and adapted for government asset management workflows.

---

## Changelog

**Latest:** See individual module documentation for changelog entries. This project follows semantic versioning and maintains a detailed history in each module's README.

---

**AMIS is designed for production-focused teams that need fast delivery with strong architecture discipline.**

