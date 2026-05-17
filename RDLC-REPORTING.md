# RDLC Reporting — Implementation Guide & Progress Tracker

> All RDLC rendering — engine, helpers, `.rdlc` templates, handlers, and endpoints — lives in **`Modules.RdlcReporting`**.
> Other modules never reference the RDLC engine. They expose their data via `.Contracts` queries, and `Modules.RdlcReporting` consumes those queries through Mediator.

---

## Architecture

```
src/Modules/RdlcReporting/
├── Modules.RdlcReporting.Contracts/           (Mediator marker — public DTOs only)
│   ├── Modules.RdlcReporting.Contracts.csproj
│   └── RdlcReportingContractsMarker.cs
└── Modules.RdlcReporting/                     (engine + .rdlc + handlers)
    ├── Modules.RdlcReporting.csproj           (refs ReportViewerCore.NETCore + consuming modules' .Contracts)
    ├── AssemblyInfo.cs                        ([assembly: AmisModule(typeof(RdlcReportingModule), 910)])
    ├── RdlcReportingModule.cs                 (IModule — DI + endpoint mapping)
    ├── RdlcReportingModuleConstants.cs        (permission strings)
    ├── Services/
    │   ├── RdlcReportHelper.cs                (static Render() — the only render entry point)
    │   └── GovernmentPaperSizes.cs            (named (Width, Height) tuples for PH gov forms)
    ├── Reports/
    │   └── {ReportName}.rdlc                  (EmbeddedResource — all .rdlc files live here)
    └── Features/v1/{Area}/{Print{Entity}}/
        ├── Print{Entity}Query.cs              (IQuery<byte[]>)
        ├── Print{Entity}QueryHandler.cs       (Mediator.Send → flat records → RdlcReportHelper.Render)
        └── Print{Entity}Endpoint.cs           (MapGet → Results.File(...))
```

**Module boundary rule:** `Modules.RdlcReporting` may reference *any* other module's `.Contracts` project — but never an implementation project, DbContext, or domain entity. Every piece of data fetched in a handler must come through `IMediator.Send(...)` against a Contracts query.

---

## File Naming Convention

Strict, repeatable names — these are the only forms accepted:

| Artifact | Pattern | Concrete example |
|---|---|---|
| Implementation project | `Modules.RdlcReporting.csproj` | `src/Modules/RdlcReporting/Modules.RdlcReporting/Modules.RdlcReporting.csproj` |
| Contracts project | `Modules.RdlcReporting.Contracts.csproj` | `src/Modules/RdlcReporting/Modules.RdlcReporting.Contracts/Modules.RdlcReporting.Contracts.csproj` |
| Implementation namespace | `AMIS.Modules.RdlcReporting{.SubArea}` | `AMIS.Modules.RdlcReporting.Services` |
| Contracts namespace | `AMIS.Modules.RdlcReporting.Contracts{.v1.{Area}}` | `AMIS.Modules.RdlcReporting.Contracts` |
| Module class | `RdlcReportingModule.cs` | implements `IModule` |
| Module constants | `RdlcReportingModuleConstants.cs` | `internal static class RdlcReportingModuleConstants` |
| Contracts marker | `RdlcReportingContractsMarker.cs` | `public sealed class RdlcReportingContractsMarker;` |
| `.rdlc` template | `{ReportName}.rdlc` (PascalCase, no spaces) | `Reports/PurchaseRequest.rdlc` |
| Embedded resource name | `AMIS.Modules.RdlcReporting.Reports.{ReportName}.rdlc` | `AMIS.Modules.RdlcReporting.Reports.PurchaseRequest.rdlc` |
| Feature folder | `Features/v1/{Area}/Print{Entity}/` | `Features/v1/PurchaseRequests/PrintPurchaseRequest/` |
| Query record | `Print{Entity}Query.cs` | `PrintPurchaseRequestQuery.cs` → `record PrintPurchaseRequestQuery : IQuery<byte[]>` |
| Query handler | `Print{Entity}QueryHandler.cs` | `PrintPurchaseRequestQueryHandler.cs` → `IQueryHandler<PrintPurchaseRequestQuery, byte[]>` |
| Endpoint | `Print{Entity}Endpoint.cs` | `PrintPurchaseRequestEndpoint.cs` → `public static class PrintPurchaseRequestEndpoint` |
| Route group root | `api/v{version:apiVersion}/rdlc-reporting` | mapped in `RdlcReportingModule.MapEndpoints` |
| Sub-group | `/{kebab-area}` under the module group | `/procurement/purchase-requests` |
| Endpoint name (`WithName`) | `RdlcReporting_{Action}{Entity}` (globally unique) | `RdlcReporting_PrintPurchaseRequest` |
| Flat record (data binding) | `Pr{Entity}{Section}` — `internal sealed record` | `PrReportHeader`, `PrReportLineItem`, `PrReportOrgProfile` |

The flat-record naming above is a soft convention — the binding contract is the *property names*, which must match the RDLC DataSet field names exactly. The C# type name itself is not constrained.

---

## Package

| Package | Version | Source |
|---|---|---|
| `ReportViewerCore.NETCore` | 15.1.33 | nuget.org |

`ReportViewerCore.NETCore` is API-compatible with the historical `Microsoft.Reporting.NETCore`. Same namespace (`Microsoft.Reporting.NETCore`), same types (`LocalReport`, `ReportDataSource`). It is the community-maintained public port and is the only RDLC dependency in the solution.

Declared once in `src/Directory.Packages.props`. Referenced **only** by `Modules.RdlcReporting.csproj` — never by any other module.

---

## `RdlcReportHelper.Render()` Signature

```csharp
namespace AMIS.Modules.RdlcReporting.Services;

public static byte[] Render(
    Assembly assembly,        // typeof(YourHandler).Assembly
    string resourceName,      // "AMIS.Modules.RdlcReporting.Reports.{Name}.rdlc"
    IEnumerable<(string Name, IEnumerable Data)> dataSources,
    string format = "PDF",
    string? pageWidth = null,
    string? pageHeight = null,
    string margin = "0.5cm")
```

Example call from `PrintPurchaseRequestQueryHandler`:

```csharp
return RdlcReportHelper.Render(
    Assembly,
    "AMIS.Modules.RdlcReporting.Reports.PurchaseRequest.rdlc",
    [
        ("PurchaseRequestDS",     (IEnumerable)headerData),
        ("LineItemsDS",           (IEnumerable)lineItems),
        ("LineItemsDS_R",         (IEnumerable)lineItems),
        ("OrganizationProfileDS", (IEnumerable)orgData),
    ],
    pageWidth:  query.PageWidth,
    pageHeight: query.PageHeight);
```

---

## `GovernmentPaperSizes` Reference

```csharp
namespace AMIS.Modules.RdlcReporting.Services;
```

| Name | Width | Height | Common use |
|---|---|---|---|
| `A4` | 21cm | 29.7cm | Standard international |
| `Legal` | 8.5in | 14in | US/long legal |
| `LongBond` | 8.5in | 13in | PH government default |
| `HalfA4` | 14.85cm | 21cm | Half-sheet A4 |
| `HalfLegal` | 7in | 8.5in | Half legal |
| `HalfLong` | 6.5in | 8.5in | Half long bond |

Standard endpoint shorthand mapping (copy into each Print endpoint):

```csharp
(pageWidth, pageHeight) = pageWidth?.ToLowerInvariant() switch
{
    "a4"        => GovernmentPaperSizes.A4,
    "legal"     => GovernmentPaperSizes.Legal,
    "longbond"  => GovernmentPaperSizes.LongBond,
    "halfa4"    => GovernmentPaperSizes.HalfA4,
    "halflegal" => GovernmentPaperSizes.HalfLegal,
    "halflong"  => GovernmentPaperSizes.HalfLong,
    _           => (pageWidth, pageHeight)
};
```

---

## Adding a New RDLC Report — Checklist

1. **Source data** — confirm the data query and DTOs you need are public in the source module's `.Contracts` project (e.g. `AMIS.Modules.{Source}.Contracts.v1.{Area}`). If not, move them there first.
2. **Project reference** — add `<ProjectReference Include="..\..\{Source}\Modules.{Source}.Contracts\Modules.{Source}.Contracts.csproj" />` to `Modules.RdlcReporting.csproj` (if not already present).
3. **Template** — design `.rdlc` in Visual Studio's Report Designer; place at `src/Modules/RdlcReporting/Modules.RdlcReporting/Reports/{ReportName}.rdlc`. (The csproj already embeds all `Reports\*.rdlc`.)
4. **Feature slice** — create `Features/v1/{Area}/Print{Entity}/` with:
   - `Print{Entity}Query.cs` — `public sealed record Print{Entity}Query(Guid Id, string? PageWidth = null, string? PageHeight = null) : IQuery<byte[]>;`
   - `Print{Entity}QueryHandler.cs` — uses `IMediator` to fetch via Contracts queries, builds flat records, calls `RdlcReportHelper.Render(...)`. Define `internal sealed record` types whose property names match the RDLC DataSet fields exactly (case-sensitive).
   - `Print{Entity}Endpoint.cs` — `MapGet("/{id:guid}/print", ...)`, `WithName("RdlcReporting_Print{Entity}")`, paper-size shorthand switch, returns `Results.File(bytes, "application/pdf", "{File}-{id}.pdf")`.
5. **Wire endpoint** — in `RdlcReportingModule.MapEndpoints`, under the appropriate `{kebab-area}` sub-group, call `Print{Entity}Endpoint.Map(...)`.
6. **API client** — add `GetReportPdfAsync` (or similar) in the relevant Blazor `*Client.cs`, targeting `api/v1/rdlc-reporting/{kebab-area}/{id}/print`.
7. **Blazor page** — open the returned bytes as a data URL in a new tab (see `PurchaseRequestPrintPage.razor` for the canonical pattern).
8. **Verify** — `dotnet build src/AMIS.Framework.slnx` (0 errors), then check the endpoint loads at `/api/v1/rdlc-reporting/...`.

---

## Reports Implemented

| Report | Source Module | Template | Status |
|---|---|---|---|
| Purchase Request (PR) | `Modules.ProcurementAcquisition` | `Reports/PurchaseRequest.rdlc` | ✅ Done |

---

## Reference Implementation

| Artifact | Path |
|---|---|
| Helper | [src/Modules/RdlcReporting/Modules.RdlcReporting/Services/RdlcReportHelper.cs](src/Modules/RdlcReporting/Modules.RdlcReporting/Services/RdlcReportHelper.cs) |
| Paper sizes | [src/Modules/RdlcReporting/Modules.RdlcReporting/Services/GovernmentPaperSizes.cs](src/Modules/RdlcReporting/Modules.RdlcReporting/Services/GovernmentPaperSizes.cs) |
| Module | [src/Modules/RdlcReporting/Modules.RdlcReporting/RdlcReportingModule.cs](src/Modules/RdlcReporting/Modules.RdlcReporting/RdlcReportingModule.cs) |
| Query | [src/Modules/RdlcReporting/Modules.RdlcReporting/Features/v1/PurchaseRequests/PrintPurchaseRequest/PrintPurchaseRequestQuery.cs](src/Modules/RdlcReporting/Modules.RdlcReporting/Features/v1/PurchaseRequests/PrintPurchaseRequest/PrintPurchaseRequestQuery.cs) |
| Handler | [src/Modules/RdlcReporting/Modules.RdlcReporting/Features/v1/PurchaseRequests/PrintPurchaseRequest/PrintPurchaseRequestQueryHandler.cs](src/Modules/RdlcReporting/Modules.RdlcReporting/Features/v1/PurchaseRequests/PrintPurchaseRequest/PrintPurchaseRequestQueryHandler.cs) |
| Endpoint | [src/Modules/RdlcReporting/Modules.RdlcReporting/Features/v1/PurchaseRequests/PrintPurchaseRequest/PrintPurchaseRequestEndpoint.cs](src/Modules/RdlcReporting/Modules.RdlcReporting/Features/v1/PurchaseRequests/PrintPurchaseRequest/PrintPurchaseRequestEndpoint.cs) |
| RDLC | [src/Modules/RdlcReporting/Modules.RdlcReporting/Reports/PurchaseRequest.rdlc](src/Modules/RdlcReporting/Modules.RdlcReporting/Reports/PurchaseRequest.rdlc) |
| Blazor page | [src/Playground/Playground.Blazor/Components/Pages/Procurement/PurchaseRequestPrintPage.razor](src/Playground/Playground.Blazor/Components/Pages/Procurement/PurchaseRequestPrintPage.razor) |
| API client | [src/Playground/Playground.Blazor/ApiClient/ProcurementClient.cs](src/Playground/Playground.Blazor/ApiClient/ProcurementClient.cs) — `GetPrintPdfAsync` |

---

## Notes

- `Microsoft.Reporting.NETCore` is NOT on nuget.org. AMIS uses `ReportViewerCore.NETCore` (same API).
- Do NOT add `Microsoft.Reporting.NETCore` back to `Directory.Packages.props` — `CentralPackageTransitivePinningEnabled=true` will break restore across all projects.
- `Reports/*.rdlc` is embedded in `Modules.RdlcReporting` only. Other modules must not embed `.rdlc` files of their own.
- The peer FastReport module follows the same conventions — see [FAST-REPORTING.md](FAST-REPORTING.md).
