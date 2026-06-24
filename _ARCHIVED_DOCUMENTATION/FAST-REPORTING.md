# Fast Reporting — Implementation Guide & Progress Tracker

> All FastReport rendering — engine, `.frx` templates, handlers, and endpoints — lives in **`Modules.FastReporting`**.
> Other modules never reference FastReport. They expose their data via `.Contracts` queries, and `Modules.FastReporting` consumes those queries through Mediator.

This is the peer of [`Modules.RdlcReporting`](RDLC-REPORTING.md). The two modules are independent: a report is either RDLC *or* FastReport, never both — pick the engine that best fits the layout requirements and stick with one per report.

---

## Architecture

```
Blazor Report Page
      │
      │  HTTP GET /api/v1/fast-reporting/{kebab-area}/.../{id}/print?...
      ▼
src/Modules/FastReporting/
├── Modules.FastReporting.Contracts/           (Mediator marker + shared report DTOs)
│   ├── Modules.FastReporting.Contracts.csproj
│   ├── FastReportingContractsMarker.cs
│   └── v1/Reports/
│       ├── ReportFileDto.cs                   (byte[] Content, ContentType, FileName)
│       └── ReportFormat.cs                    (enum: Pdf, Excel)
└── Modules.FastReporting/                     (engine + .frx + handlers)
    ├── Modules.FastReporting.csproj           (refs FastReport.OpenSource + consuming modules' .Contracts)
    ├── AssemblyInfo.cs                        ([assembly: AmisModule(typeof(FastReportingModule), 900)])
    ├── FastReportingModule.cs                 (IModule — DI + endpoint mapping)
    ├── FastReportingModuleConstants.cs        (permission strings)
    ├── Services/
    │   └── FastReportService.cs               (load .frx → register data → export PDF)
    ├── Templates/
    │   └── {ReportName}.frx                   (EmbeddedResource — all .frx files live here)
    └── Features/v1/{Area}/{Print{Entity}Fast}/
        ├── Print{Entity}FastQuery.cs          (IQuery<byte[]>)
        ├── Print{Entity}FastQueryHandler.cs   (Mediator.Send → DataTable/records → FastReport export)
        └── Print{Entity}FastEndpoint.cs       (MapGet → Results.File(...))
```

**Module boundary rule:** `Modules.FastReporting` may reference *any* other module's `.Contracts` project — but never an implementation project, DbContext, or domain entity. Every piece of data fetched in a handler must come through `IMediator.Send(...)` against a Contracts query.

---

## File Naming Convention

Strict, repeatable names — these are the only forms accepted:

| Artifact | Pattern | Concrete example |
|---|---|---|
| Implementation project | `Modules.FastReporting.csproj` | `src/Modules/FastReporting/Modules.FastReporting/Modules.FastReporting.csproj` |
| Contracts project | `Modules.FastReporting.Contracts.csproj` | `src/Modules/FastReporting/Modules.FastReporting.Contracts/Modules.FastReporting.Contracts.csproj` |
| Implementation namespace | `AMIS.Modules.FastReporting{.SubArea}` | `AMIS.Modules.FastReporting.Services` |
| Contracts namespace | `AMIS.Modules.FastReporting.Contracts{.v1.{Area}}` | `AMIS.Modules.FastReporting.Contracts.v1.Reports` |
| Module class | `FastReportingModule.cs` | implements `IModule` |
| Module constants | `FastReportingModuleConstants.cs` | `internal static class FastReportingModuleConstants` |
| Contracts marker | `FastReportingContractsMarker.cs` | `public sealed class FastReportingContractsMarker;` |
| `.frx` template | `{ReportName}.frx` (PascalCase, no spaces) | `Templates/PurchaseRequestFast.frx` |
| Embedded resource name | `AMIS.Modules.FastReporting.Templates.{ReportName}.frx` | `AMIS.Modules.FastReporting.Templates.PurchaseRequestFast.frx` |
| Feature folder | `Features/v1/{Area}/Print{Entity}Fast/` | `Features/v1/PurchaseRequests/PrintPurchaseRequestFast/` |
| Query record | `Print{Entity}FastQuery.cs` | `PrintPurchaseRequestFastQuery.cs` → `record PrintPurchaseRequestFastQuery : IQuery<byte[]>` |
| Query handler | `Print{Entity}FastQueryHandler.cs` | `PrintPurchaseRequestFastQueryHandler.cs` → `IQueryHandler<PrintPurchaseRequestFastQuery, byte[]>` |
| Endpoint | `Print{Entity}FastEndpoint.cs` | `PrintPurchaseRequestFastEndpoint.cs` → `public static class PrintPurchaseRequestFastEndpoint` |
| Route group root | `api/v{version:apiVersion}/fast-reporting` | mapped in `FastReportingModule.MapEndpoints` |
| Sub-group | `/{kebab-area}` under the module group | `/procurement/purchase-requests` |
| Endpoint name (`WithName`) | `FastReporting_{Action}{Entity}` (globally unique) | `FastReporting_PrintPurchaseRequest` |
| Flat record (data binding) | `Pr{Entity}{Section}` — `internal sealed record` | `PrFastHeader`, etc. |

The `Fast` suffix on every feature artifact is required — it distinguishes the FastReport variant from the RDLC variant of the same report (e.g. RDLC's `PrintPurchaseRequestQuery` vs FastReport's `PrintPurchaseRequestFastQuery`). The endpoint *name* (used in `WithName(...)`) drops the suffix because the module prefix `FastReporting_` already disambiguates.

The flat-record naming is a soft convention — the binding contract is the *property names*, which must match the FastReport data-source field names exactly. The C# type name itself is not constrained.

---

## NuGet Packages

Declared in `src/Directory.Packages.props`. Referenced **only** by `Modules.FastReporting.csproj` — never by any other module.

| Package | Purpose |
|---|---|
| `FastReport.OpenSource` | Report engine — loads `.frx`, prepares data |
| `FastReport.OpenSource.Export.PdfSimple` | PDF export — no native dependencies |

> To add Excel export later: `FastReport.OpenSource.Export.OoXML`.

---

## `FastReportService` Signature

```csharp
namespace AMIS.Modules.FastReporting.Services;

public static class FastReportService
{
    public static Task<ReportFileDto> GenerateAsync(
        string templateName,                     // matches "{ReportName}" — no .frx suffix
        IEnumerable<ReportDataSource> sources,
        ReportFormat format = ReportFormat.Pdf,
        CancellationToken ct = default);
}

public sealed record ReportDataSource(string Name, IEnumerable Data);
```

Internally `GenerateAsync` loads the template via `AMIS.Modules.FastReporting.Templates.{templateName}.frx` from the assembly's embedded resources.

For richer scenarios (custom paper sizes, multi-copy layouts, hidden right-side bands), call FastReport directly inside the handler — see `PrintPurchaseRequestFastQueryHandler` for the canonical complex case (paper-size switching, single-vs-two-copy mode, `DataTable`-backed line items with row padding).

---

## Adding a New FastReport Report — Checklist

1. **Source data** — confirm the data query and DTOs you need are public in the source module's `.Contracts` project (e.g. `AMIS.Modules.{Source}.Contracts.v1.{Area}`). If not, move them there first.
2. **Project reference** — add `<ProjectReference Include="..\..\{Source}\Modules.{Source}.Contracts\Modules.{Source}.Contracts.csproj" />` to `Modules.FastReporting.csproj` (if not already present).
3. **Template** — design `.frx` in the **FastReport Open Source Designer** (a separate free tool). Place at `src/Modules/FastReporting/Modules.FastReporting/Templates/{ReportName}.frx`. (The csproj already embeds all `Templates\*.frx`.)
4. **Feature slice** — create `Features/v1/{Area}/Print{Entity}Fast/` with:
   - `Print{Entity}FastQuery.cs` — `public sealed record Print{Entity}FastQuery(Guid Id, /* paper params */) : IQuery<byte[]>;`
   - `Print{Entity}FastQueryHandler.cs` — uses `IMediator` to fetch via Contracts queries, builds records or a `DataTable`, loads the embedded `.frx`, calls `report.RegisterData(...)`, applies paper-size logic, prepares, and exports to `byte[]`.
   - `Print{Entity}FastEndpoint.cs` — `MapGet("/{id:guid}/print", ...)`, `WithName("FastReporting_Print{Entity}")`, returns `Results.File(bytes, "application/pdf", "{File}-{id}.pdf")`.
5. **Wire endpoint** — in `FastReportingModule.MapEndpoints`, under the appropriate `{kebab-area}` sub-group, call `Print{Entity}FastEndpoint.Map(...)`.
6. **API client** — add `GetFastReportPdfAsync` (or similar) in the relevant Blazor `*Client.cs`, targeting `api/v1/fast-reporting/{kebab-area}/{id}/print`.
7. **Blazor page** — open the returned bytes as a data URL in a new tab.
8. **Verify** — `dotnet build src/AMIS.Framework.slnx` (0 errors), then check the endpoint loads at `/api/v1/fast-reporting/...`.

---

## Reports Implemented

Legend: ✅ Done · 🔧 In Progress · ⬜ Pending

### Procurement
| Report | Source Module | Template | Status |
|---|---|---|---|
| Purchase Request (PR) — landscape, 1 or 2 copies | `Modules.ProcurementAcquisition` | `Templates/PurchaseRequestFast.frx` | ✅ Done |
| Purchase Order (PO) | `Modules.ProcurementAcquisition` | ⬜ | ⬜ |

### Asset Management
| Report | Source Module | Template | Status |
|---|---|---|---|
| Semi-Expendable Property Card (SPC) | `Modules.AssetManagement` | ⬜ | ⬜ |
| Registry of SE Property Issued (RegSPI) | `Modules.AssetManagement` | ⬜ | ⬜ |
| Report of SE Property Issued (RSPI) | `Modules.AssetManagement` | ⬜ | ⬜ |
| Property History | `Modules.AssetManagement` | ⬜ | ⬜ |
| PPE Issuance / Receiving / Transfer | `Modules.AssetManagement` | ⬜ | ⬜ |
| Property Incident | `Modules.AssetManagement` | ⬜ | ⬜ |
| Receiving (RPCPPE) | `Modules.AssetManagement` | ⬜ | ⬜ |
| Unserviceable Property | `Modules.AssetManagement` | ⬜ | ⬜ |

### Asset Register
| Report | Source Module | Template | Status |
|---|---|---|---|
| Registry of SE Property Issued (RegSPI) | `Modules.AssetRegister` | ⬜ | ⬜ |
| Report of PPE Custodianship (RPCPPE) | `Modules.AssetRegister` | ⬜ | ⬜ |
| Report of Semi-Expendable (RPCSEMEX) | `Modules.AssetRegister` | ⬜ | ⬜ |
| ICS / PAR | `Modules.AssetRegister` | ⬜ | ⬜ |
| PPE Receiving / SMRR Receiving | `Modules.AssetRegister` | ⬜ | ⬜ |
| Incident / Unserviceable | `Modules.AssetRegister` | ⬜ | ⬜ |

### Expendable
| Report | Source Module | Template | Status |
|---|---|---|---|
| Department Issuance | `Modules.Expendable` | ⬜ | ⬜ |
| Physical Count | `Modules.Expendable` | ⬜ | ⬜ |

### Vehicle
| Report | Source Module | Template | Status |
|---|---|---|---|
| Vehicle Inventory | `Modules.Vehicle` | ⬜ | ⬜ |

### Procurement Planning
| Report | Source Module | Template | Status |
|---|---|---|---|
| PPMP | `Modules.ProcurementPlanning` | ⬜ | ⬜ |

### Finance
| Report | Source Module | Template | Status |
|---|---|---|---|
| Disbursement Voucher | `Modules.Finance` | ⬜ | ⬜ |

---

## Permissions

Defined in `FastReportingModuleConstants.Permissions`:

| Constant | String value |
|---|---|
| `ViewAssetReports` | `fastreporting.assets.view` |
| `ViewExpenditureReports` | `fastreporting.expendable.view` |
| `ViewVehicleReports` | `fastreporting.vehicle.view` |
| `ViewProcurementReports` | `fastreporting.procurement.view` |
| `ViewFinanceReports` | `fastreporting.finance.view` |

These are scoped by report domain, not by individual report. For per-feature gating, reuse the owning module's permission instead (e.g. `Permissions.Procurement.PurchaseRequests.View` on the PR print endpoint).

---

## FastReport Designer

The FastReport Open Source Designer is a **separate free tool**:

- Download: <https://github.com/FastReports/FastReport/releases>
- Use it to design `.frx` templates visually.
- The data source in the designer must match the property names of the records/DataTable columns you register at runtime.
- `[Report] → Data → Add Data Source → Business Object` and point it at your DTO class structure, or use a `DataTable` for variable-row tabular data (see `PrintPurchaseRequestFastQueryHandler` for the canonical pattern).

---

## Reference Implementation

| Artifact | Path |
|---|---|
| Service | [src/Modules/FastReporting/Modules.FastReporting/Services/FastReportService.cs](src/Modules/FastReporting/Modules.FastReporting/Services/FastReportService.cs) |
| Module | [src/Modules/FastReporting/Modules.FastReporting/FastReportingModule.cs](src/Modules/FastReporting/Modules.FastReporting/FastReportingModule.cs) |
| Query | [src/Modules/FastReporting/Modules.FastReporting/Features/v1/PurchaseRequests/PrintPurchaseRequestFast/PrintPurchaseRequestFastQuery.cs](src/Modules/FastReporting/Modules.FastReporting/Features/v1/PurchaseRequests/PrintPurchaseRequestFast/PrintPurchaseRequestFastQuery.cs) |
| Handler | [src/Modules/FastReporting/Modules.FastReporting/Features/v1/PurchaseRequests/PrintPurchaseRequestFast/PrintPurchaseRequestFastQueryHandler.cs](src/Modules/FastReporting/Modules.FastReporting/Features/v1/PurchaseRequests/PrintPurchaseRequestFast/PrintPurchaseRequestFastQueryHandler.cs) |
| Endpoint | [src/Modules/FastReporting/Modules.FastReporting/Features/v1/PurchaseRequests/PrintPurchaseRequestFast/PrintPurchaseRequestFastEndpoint.cs](src/Modules/FastReporting/Modules.FastReporting/Features/v1/PurchaseRequests/PrintPurchaseRequestFast/PrintPurchaseRequestFastEndpoint.cs) |
| Template | [src/Modules/FastReporting/Modules.FastReporting/Templates/PurchaseRequestFast.frx](src/Modules/FastReporting/Modules.FastReporting/Templates/PurchaseRequestFast.frx) |
| API client | [src/Host/AMIS.Blazor/ApiClient/ProcurementClient.cs](src/Host/AMIS.Blazor/ApiClient/ProcurementClient.cs) — `GetFastReportPdfAsync` |

---

## Notes

- `Templates/Placeholder.frx` is intentionally kept as a minimal embedded resource so the `<EmbeddedResource Include="Templates\*.frx" />` glob always matches at least one file.
- The peer RDLC module follows the same conventions — see [RDLC-REPORTING.md](RDLC-REPORTING.md).
