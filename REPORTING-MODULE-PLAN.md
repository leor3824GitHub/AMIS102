# Reporting Module — Implementation Plan & Progress Tracker

> Centralized FastReport Open Source reporting for all AMIS modules.
> All report endpoints live under `/api/v1/reporting/`. Data is fetched via Mediator queries — no direct DbContext cross-module access.

---

## Architecture

```
Blazor Report Page
      │
      │  HTTP GET /api/v1/reporting/{report}?params&format=pdf
      ▼
Modules.Reporting
  ├── ReportingModule          (IModule — DI + endpoints)
  ├── FastReportService        (loads .frx, injects data, exports PDF/Excel)
  ├── Templates/*.frx          (embedded resources — design with FastReport Designer)
  └── Features/v1/Reports/
        └── {ReportName}/
              ├── {ReportName}Query.cs         (IQuery<ReportFileDto>)
              ├── {ReportName}QueryHandler.cs  (fetches data via Mediator → FastReportService)
              └── {ReportName}Endpoint.cs      (MapGet → returns File result)
```

### Key Rule: Data fetching via Mediator only

The Reporting module **cannot** reference any other module's DbContext directly.
To call another module's query, that query **must** live in its `.Contracts` project.

**Before implementing each report handler:**
1. Check if the source query is in the relevant module's `.Contracts` project.
2. If not → move it (or add a reporting-specific query) to the Contracts project first.
3. Then call it from the Reporting handler via `IMediator.Send(...)`.

---

## NuGet Packages

Added to `src/Directory.Packages.props`:

| Package | Purpose |
|---|---|
| `FastReport.OpenSource` | Report engine — loads `.frx`, prepares data |
| `FastReport.OpenSource.Export.PdfSimple` | PDF export — no native dependencies |

> To add Excel export later: `FastReport.OpenSource.Export.OoXML`

---

## Module Projects

```
src/Modules/Reporting/
├── Modules.Reporting.Contracts/    (Mediator marker + shared DTOs)
│   ├── ReportingContractsMarker.cs
│   └── v1/Reports/
│       ├── ReportFileDto.cs        (byte[] Content, ContentType, FileName)
│       └── ReportFormat.cs         (enum: Pdf, Excel)
└── Modules.Reporting/              (FastReport engine + all report handlers)
    ├── AssemblyInfo.cs
    ├── ReportingModule.cs
    ├── ReportingModuleConstants.cs
    ├── Services/
    │   └── FastReportService.cs
    ├── Templates/                   ← .frx files (EmbeddedResource)
    └── Features/v1/Reports/
        └── {ReportName}/
```

---

## Report Inventory & Progress

Legend: ✅ Done · 🔧 In Progress · ⬜ Pending · ⚠️ Needs Contracts migration

### Phase 1 — Module Infrastructure
| Task | Status |
|---|---|
| Create `Modules.Reporting.Contracts` project | ✅ Done |
| Create `Modules.Reporting` project | ✅ Done |
| Implement `FastReportService` | ✅ Done |
| Add FastReport NuGet packages | ✅ Done |
| Wire into `AMIS.Framework.slnx` | ✅ Done |
| Wire into `Playground.Api.csproj` | ✅ Done |
| Wire into `Program.cs` | ✅ Done |
| Build passes with 0 warnings | 🔧 In Progress |

---

### Phase 2 — Asset Management Reports

> Source module: `Modules.AssetManagement`
> Contracts migration needed: move report queries to `Modules.AssetManagement.Contracts`

| Report | Template | Handler | Endpoint | Blazor Page | Status |
|---|---|---|---|---|---|
| Semi-Expendable Property Card (SPC) | ⬜ | ⬜ | ⬜ | `AssetManagementReportsPage` | ⬜ |
| Registry of SE Property Issued (RegSPI) | ⬜ | ⬜ | ⬜ | `AssetManagementReportsPage` | ⬜ |
| Report of SE Property Issued (RSPI) | ⬜ | ⬜ | ⬜ | `AssetManagementReportsPage` | ⬜ |
| Property History | ⬜ | ⬜ | ⬜ | `AssetManagementReportsPage` | ⬜ |
| PPE Issuance Report | ⬜ | ⬜ | ⬜ | `PPEIssuanceReportsPage` | ⬜ |
| PPE Receiving Report | ⬜ | ⬜ | ⬜ | `PPEReceivingReportsPage` | ⬜ |
| PPE Transfer Report | ⬜ | ⬜ | ⬜ | `PPETransferReportsPage` | ⬜ |
| Property Incident Report | ⬜ | ⬜ | ⬜ | `PropertyIncidentReportsPage` | ⬜ |
| Receiving Report (RPCPPE) | ⬜ | ⬜ | ⬜ | `ReceivingReportsPage` | ⬜ |
| Unserviceable Property Report | ⬜ | ⬜ | ⬜ | `UnserviceablePropertyReportsPage` | ⬜ |

**Contracts to migrate from `Modules.AssetManagement` → `Modules.AssetManagement.Contracts`:**
- `GetSPCQuery` + response DTOs
- `GetRegSPIQuery` + response DTOs
- `GetRSPIQuery` + response DTOs
- `GetPropertyHistoryQuery` + response DTOs

---

### Phase 3 — Asset Register Reports

> Source module: `Modules.AssetRegister`
> Contracts migration needed for all queries below.

| Report | Template | Handler | Endpoint | Blazor Page | Status |
|---|---|---|---|---|---|
| Registry of SE Property Issued (RegSPI) | ⬜ | ⬜ | ⬜ | `AssetRegisterReportsPage` | ⬜ |
| Report of PPE Custodianship (RPCPPE) | ⬜ | ⬜ | ⬜ | `AssetRegisterReportsPage` | ⬜ |
| Report of Semi-Expendable (RPCSEMEX) | ⬜ | ⬜ | ⬜ | `AssetRegisterReportsPage` | ⬜ |
| ICS Report | ⬜ | ⬜ | ⬜ | `IssuanceReportsPage` | ⬜ |
| PAR Report | ⬜ | ⬜ | ⬜ | `IssuanceReportsPage` | ⬜ |
| PPE Receiving Report (RPCPPE) | ⬜ | ⬜ | ⬜ | `PpeReceivingReportsPage` | ⬜ |
| SMRR Receiving Report | ⬜ | ⬜ | ⬜ | `SmrrReceivingReportsPage` | ⬜ |
| Incident Report | ⬜ | ⬜ | ⬜ | `IncidentReportsPage` | ⬜ |
| Unserviceable Property Report | ⬜ | ⬜ | ⬜ | `UnserviceableReportsPage` | ⬜ |

---

### Phase 4 — Expendable Reports

> Source module: `Modules.Expendable`

| Report | Template | Handler | Endpoint | Blazor Page | Status |
|---|---|---|---|---|---|
| Department Issuance Report | ⬜ | ⬜ | ⬜ | `DepartmentIssuanceReportPage` | ⬜ |
| Physical Count Report | ⬜ | ⬜ | ⬜ | `PhysicalCountReportPage` | ⬜ |

---

### Phase 5 — Vehicle Reports

> Source module: `Modules.Vehicle`

| Report | Template | Handler | Endpoint | Blazor Page | Status |
|---|---|---|---|---|---|
| Vehicle Inventory Report | ⬜ | ⬜ | ⬜ | `VehicleInventoryReportPage` | ⬜ |

---

### Phase 6 — Procurement Reports

> Source modules: `Modules.ProcurementAcquisition`, `Modules.ProcurementPlanning`

| Report | Template | Handler | Endpoint | Blazor Page | Status |
|---|---|---|---|---|---|
| Purchase Request (PR) Print | ⬜ | ⬜ | ⬜ | TBD | ⬜ |
| Purchase Order (PO) Print | ⬜ | ⬜ | ⬜ | TBD | ⬜ |
| PPMP Report | ⬜ | ⬜ | ⬜ | TBD | ⬜ |

---

### Phase 7 — Finance Reports

> Source module: `Modules.Finance`

| Report | Template | Handler | Endpoint | Blazor Page | Status |
|---|---|---|---|---|---|
| Disbursement Voucher Print | ⬜ | ⬜ | ⬜ | TBD | ⬜ |

---

## How to Add a New Report

### Step 1 — Move the query to Contracts (if not already there)

Move or create the report data query + response DTOs in the **source module's Contracts** project:
```
src/Modules/{Module}/Modules.{Module}.Contracts/v1/Reports/{ReportName}/
├── Get{ReportName}ReportQuery.cs    ← IQuery<{ReportName}ReportDto>
└── {ReportName}ReportDto.cs         ← all response records
```

### Step 2 — Create the .frx template

1. Open **FastReport Designer** (standalone tool).
2. Create a new report, add a data source matching your DTO property names.
3. Design the layout (headers, data bands, footers, signatories).
4. Save as `{ReportName}.frx` in `Modules.Reporting/Templates/`.
5. Set **Build Action → Embedded Resource** in Visual Studio.

Or manually in the `.csproj`:
```xml
<ItemGroup>
  <EmbeddedResource Include="Templates\{ReportName}.frx" />
</ItemGroup>
```

### Step 3 — Add the vertical slice in Modules.Reporting

```
Features/v1/Reports/{ReportName}/
├── Generate{ReportName}Query.cs
├── Generate{ReportName}QueryHandler.cs
└── Generate{ReportName}Endpoint.cs
```

**Query:**
```csharp
public sealed record Generate{ReportName}Query(
    /* filter params */
    ReportFormat Format = ReportFormat.Pdf)
    : IQuery<ReportFileDto>;
```

**Handler:**
```csharp
internal sealed class Generate{ReportName}QueryHandler(
    IMediator mediator,
    FastReportService reporting)
    : IQueryHandler<Generate{ReportName}Query, ReportFileDto>
{
    public async ValueTask<ReportFileDto> Handle(
        Generate{ReportName}Query query, CancellationToken ct)
    {
        // 1. Fetch data via the source module's query (must be in their Contracts)
        var data = await mediator.Send(new Get{Source}ReportQuery(/* params */), ct);

        // 2. Generate report
        return await reporting.GenerateAsync(
            templateName: "{ReportName}",
            sources: [new("DataSource", data.Items)],
            format: query.Format,
            ct: ct);
    }
}
```

**Endpoint:**
```csharp
public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
    endpoints.MapGet("/{kebab-name}", async (
        [AsParameters] Generate{ReportName}Query query,
        IMediator mediator, CancellationToken ct) =>
    {
        var result = await mediator.Send(query, ct);
        return Results.File(result.Content, result.ContentType, result.FileName);
    })
    .WithName("Reporting_Generate{ReportName}")
    .WithSummary("Generate {Report Name} as PDF or Excel")
    .RequirePermission(ReportingModuleConstants.Permissions.View{Domain}Reports);
```

### Step 4 — Register endpoint in ReportingModule.cs

```csharp
Generate{ReportName}Endpoint.Map(reportsGroup);
```

### Step 5 — Update Blazor report page

Replace the existing data-fetch + JS-interop PDF logic with a single download link:
```csharp
// In the Blazor page
var url = $"/api/v1/reporting/{kebab-name}?{queryString}&format=pdf";
await JS.InvokeVoidAsync("open", url, "_blank");
```

---

## FastReport Designer

The FastReport Open Source Designer is a **separate free tool**:
- Download: https://github.com/FastReports/FastReport/releases
- Use it to design `.frx` templates visually.
- The data source in the designer must match the property names of the DTOs you pass in `FastReportService.GenerateAsync`.
- Use `[Report] → Data → Add Data Source → Business Object` and point it at your DTO class structure.

---

## Endpoint Convention

All reporting endpoints:
```
GET /api/v1/reporting/{domain}-{report-name}?{params}&format=pdf
```

Examples:
```
GET /api/v1/reporting/asset-spc?itemId=...&format=pdf
GET /api/v1/reporting/asset-regspi?employeeId=...&assetType=SE&format=pdf
GET /api/v1/reporting/expendable-department-issuance?from=...&to=...&departmentId=...&format=pdf
GET /api/v1/reporting/vehicle-inventory?asOfDate=...&format=pdf
GET /api/v1/reporting/procurement-purchase-request/{id}?format=pdf
```

---

## Permissions

Defined in `ReportingModuleConstants.cs`:

| Permission Key | Scope |
|---|---|
| `reporting.assets.view` | Asset Management + Asset Register reports |
| `reporting.expendable.view` | Expendable reports |
| `reporting.vehicle.view` | Vehicle reports |
| `reporting.procurement.view` | Procurement + Planning reports |
| `reporting.finance.view` | Finance reports |

---

*Last updated: 2026-05-16*
