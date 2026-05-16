# RDLC Reporting — Implementation Guide & Progress Tracker

> Shared rendering infrastructure lives in `BuildingBlocks/Reporting`.  
> RDLC report files (`.rdlc`) stay embedded in the module that owns the data.

---

## Architecture

```
BuildingBlocks/Reporting/
├── Reporting.csproj              ← ReportViewerCore.NETCore (15.1.33)
├── RdlcReportHelper.cs           ← static Render() — the only entry point
└── GovernmentPaperSizes.cs       ← named (Width, Height) tuples for PH gov forms

Modules/{Module}/
├── Reports/
│   └── {ReportName}.rdlc         ← embedded resource — STAYS in the module
└── Features/v1/{Area}/Print.../
    ├── Print{Entity}Query.cs
    ├── Print{Entity}QueryHandler.cs  ← calls RdlcReportHelper.Render(...)
    └── Print{Entity}Endpoint.cs      ← GovernmentPaperSizes shorthand mapping
```

**Rule:** `BuildingBlocks.Reporting` handles *how* to render.  
Each module handles *what* to render (data, RDLC file, paper size selection).

---

## Package

| Package | Version | Source |
|---------|---------|--------|
| `ReportViewerCore.NETCore` | 15.1.33 | nuget.org |

`ReportViewerCore.NETCore` is API-compatible with `Microsoft.Reporting.NETCore` (same namespace: `Microsoft.Reporting.NETCore`, same classes: `LocalReport`, `ReportDataSource`). It is the community-maintained public port and is available on nuget.org.

---

## `RdlcReportHelper.Render()` signature

```csharp
public static byte[] Render(
    Assembly assembly,        // typeof(YourHandler).Assembly
    string resourceName,      // "AMIS.Modules.{Module}.Reports.{Name}.rdlc"
    IEnumerable<(string Name, IEnumerable Data)> dataSources,
    string format = "PDF",
    string? pageWidth = null,
    string? pageHeight = null,
    string margin = "0.5cm")
```

### How to call it

```csharp
return RdlcReportHelper.Render(
    Assembly,
    "AMIS.Modules.ProcurementAcquisition.Reports.PurchaseRequest.rdlc",
    [
        ("PurchaseRequestDS",    (IEnumerable)headerData),
        ("LineItemsDS",          (IEnumerable)lineItems),
        ("OrganizationProfileDS",(IEnumerable)orgData),
    ],
    pageWidth:  query.PageWidth,
    pageHeight: query.PageHeight);
```

---

## `GovernmentPaperSizes` reference

| Name | Width | Height | Common use |
|------|-------|--------|------------|
| `A4` | 21cm | 29.7cm | Standard international |
| `Legal` | 8.5in | 14in | Long legal |
| `LongBond` | 8.5in | 13in | PH government default |
| `HalfA4` | 14.85cm | 21cm | Half-sheet A4 |
| `HalfLegal` | 7in | 8.5in | Half legal |
| `HalfLong` | 6.5in | 8.5in | Half long bond |

Endpoint shorthand mapping pattern (copy to each Print endpoint):
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

## Adding a new RDLC report — checklist

- [ ] Create `.rdlc` file in `Modules/{Module}/Reports/`
- [ ] Mark it `<EmbeddedResource Include="Reports\*.rdlc" />` in the `.csproj`
- [ ] Add `ProjectReference` to `BuildingBlocks/Reporting/Reporting.csproj` in the module's `.csproj`
- [ ] Create flat internal record types for RDLC data binding (in the QueryHandler file)
- [ ] Call `RdlcReportHelper.Render(...)` in the query handler
- [ ] Add `GovernmentPaperSizes` shorthand switch in the endpoint
- [ ] Add `.Produces(StatusCodes.Status200OK, null, "application/pdf")` to the endpoint metadata

---

## Reports implemented

| Report | Module | RDLC file | Status |
|--------|--------|-----------|--------|
| Purchase Request (PR) | ProcurementAcquisition | `PurchaseRequest.rdlc` | ✅ Done |

---

## Modules wired to BuildingBlocks.Reporting

| Module | csproj reference added |
|--------|----------------------|
| `Modules.ProcurementAcquisition` | ✅ Yes |

---

## Notes

- `Microsoft.Reporting.NETCore` is NOT on nuget.org. We migrated to `ReportViewerCore.NETCore` (same API).
- Do NOT add `Microsoft.Reporting.NETCore` back to `Directory.Packages.props` — `CentralPackageTransitivePinningEnabled=true` will cause restore failures across all projects.
- Each module keeps its own `.rdlc` files. Only the rendering engine is shared.
