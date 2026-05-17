---
paths:
  - "src/**/Reports/**/*.rdlc"
  - "src/**/Features/**/Print*"
  - "src/Modules/RdlcReporting/**"
---

# RDLC Reports with ReportViewerCore

AMIS uses **ReportViewerCore.NETCore** (v15.1.33) for server-side RDLC → PDF rendering. The entry point is `RdlcReportHelper.Render()` in `Modules.RdlcReporting`.

RDLC handlers, endpoints, and `.rdlc` templates live in the dedicated `Modules.RdlcReporting` module, mirroring how FastReport reports live in `Modules.FastReporting`. Other modules consume RDLC by sending mediator queries that the `RdlcReporting` handlers fulfill — they do not reference the RDLC engine themselves.

## Package

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="ReportViewerCore.NETCore" Version="15.1.33" />

<!-- Modules.RdlcReporting.csproj -->
<PackageReference Include="ReportViewerCore.NETCore" />
```

Exposes `Microsoft.Reporting.NETCore` namespace — `LocalReport`, `ReportDataSource`, etc. Same API as the official package.

## RdlcReportHelper

`src/Modules/RdlcReporting/Modules.RdlcReporting/Services/RdlcReportHelper.cs` — static utility, three steps internally:

```csharp
public static byte[] Render(
    Assembly assembly,
    string resourceName,
    IEnumerable<(string Name, IEnumerable Data)> dataSources,
    string format = "PDF",
    string? pageWidth = null,
    string? pageHeight = null,
    string margin = "0.5cm")
```

1. Loads `.rdlc` from embedded resource stream
2. Binds each `(name, data)` tuple as a `ReportDataSource`
3. Renders with optional `<DeviceInfo>` page size override; returns `byte[]`

Namespace: `AMIS.Modules.RdlcReporting.Services`

## Embed the .rdlc File

In `Modules.RdlcReporting.csproj`:

```xml
<EmbeddedResource Include="Reports\*.rdlc" />
```

Resource name pattern: `AMIS.Modules.RdlcReporting.Reports.{FileName}.rdlc`

Example: `AMIS.Modules.RdlcReporting.Reports.PurchaseRequest.rdlc`

## Handler Pattern

Reference: `PrintPurchaseRequestQueryHandler.cs` inside `Modules.RdlcReporting`.

Handlers live in `Modules.RdlcReporting/Features/v1/{Area}/{Print...}/` and use mediator to fetch data from other modules' `.Contracts` — they never reference module internals or DbContexts.

```csharp
using AMIS.Modules.RdlcReporting.Services;

namespace AMIS.Modules.RdlcReporting.Features.v1.{Area}.{PrintFeature};

public sealed class MyReportQueryHandler(IMediator mediator) : IQueryHandler<MyReportQuery, byte[]>
{
    private static readonly Assembly Assembly = typeof(MyReportQueryHandler).Assembly;
    private const string ReportResource = "AMIS.Modules.RdlcReporting.Reports.MyReport.rdlc";

    public async ValueTask<byte[]> Handle(MyReportQuery query, CancellationToken ct)
    {
        // 1. Fetch data via other modules' Contracts queries
        var dto = await mediator.Send(new GetSomethingQuery(query.Id), ct)
            ?? throw new KeyNotFoundException(...);

        // 2. Build flat records matching RDLC DataSet field names exactly
        var headerData = new List<MyReportHeader> { new(...) };
        var lineItems  = new List<MyReportLineItem> { ... };

        // 3. Render
        return RdlcReportHelper.Render(
            Assembly,
            ReportResource,
            [
                ("HeaderDS",    (IEnumerable)headerData),
                ("LineItemsDS", (IEnumerable)lineItems),
            ],
            pageWidth:  query.PageWidth,   // null = use .rdlc default
            pageHeight: query.PageHeight);
    }
}

// Flat records — property names must match RDLC DataSet field names exactly (case-sensitive)
internal sealed record MyReportHeader(string Title, string Date, ...);
internal sealed record MyReportLineItem(int No, string Description, decimal Amount);
```

## DataSet ↔ Record Contract

RDLC `DataSet` name and every field name must match C# record property names **exactly** (case-sensitive).

| RDLC DataSet name | RDLC Field | C# record property |
|---|---|---|
| `LineItemsDS` | `ItemNo` | `int ItemNo` |
| `LineItemsDS` | `EstimatedUnitCost` | `decimal EstimatedUnitCost` |

Mismatch = field renders blank in the PDF with no build error.

## Endpoint Pattern

Endpoints live in `Modules.RdlcReporting` and are mapped through `RdlcReportingModule.MapEndpoints`. The module group is `api/v1/rdlc-reporting/`, with sub-groups per consuming area (e.g. `/procurement/purchase-requests`).

```csharp
using AMIS.Modules.RdlcReporting.Services;

endpoints.MapGet("/{id:guid}/print", async (Guid id, IMediator mediator, CancellationToken ct,
    string? pageWidth = null, string? pageHeight = null) =>
{
    // Resolve shorthand paper sizes
    (pageWidth, pageHeight) = pageWidth?.ToLowerInvariant() switch
    {
        "a4"       => GovernmentPaperSizes.A4,
        "longbond" => GovernmentPaperSizes.LongBond,
        _          => (pageWidth, pageHeight)
    };

    var bytes = await mediator.Send(new MyReportQuery(id, pageWidth, pageHeight), ct);
    return Results.File(bytes, "application/pdf", $"report-{id}.pdf");
})
.WithName("RdlcReporting_PrintMyReport")    // GLOBALLY unique — prefix with RdlcReporting
.WithSummary("Generate RDLC PDF for my report")
.Produces(200, null, "application/pdf")
.RequirePermission(MyPermissions.MyEntity.View);
```

## Paper Size Shortcuts

`GovernmentPaperSizes` lives in `AMIS.Modules.RdlcReporting.Services` and defines preset `(width, height)` tuples:

| Shorthand | Width | Height |
|-----------|-------|--------|
| `a4` | 21cm | 29.7cm |
| `legal` | 8.5in | 14in |
| `longbond` | 8.5in | 13in |
| `halfa4` | 14.85cm | 21cm |
| `halflegal` | 7in | 8.5in |
| `halflong` | 6.5in | 8.5in |

## Blazor UI Integration

Fetch bytes from the API client at `api/v1/rdlc-reporting/.../print`, then open as a data URL in a new tab:

```csharp
// In API client (e.g. ProcurementClient.GetPrintPdfAsync)
public Task<byte[]> GetReportPdfAsync(Guid id, string? paperSize = null, CancellationToken ct = default)
{
    var url = string.IsNullOrWhiteSpace(paperSize)
        ? $"api/v1/rdlc-reporting/procurement/purchase-requests/{id}/print"
        : $"api/v1/rdlc-reporting/procurement/purchase-requests/{id}/print?pageWidth={paperSize}";
    return http.GetByteArrayAsync(url, ct);
}

// In Blazor page @code block
private async Task DownloadPdfAsync(string size)
{
    _loading = true;
    try
    {
        var bytes = await Client.GetReportPdfAsync(Id, size);
        var dataUrl = $"data:application/pdf;base64,{Convert.ToBase64String(bytes)}";
        await JS.InvokeVoidAsync("open", dataUrl, "_blank");
        Snackbar.Add("PDF ready.", Severity.Success);
    }
    catch (Exception ex)
    {
        Snackbar.Add($"PDF generation failed: {ex.Message}", Severity.Error);
    }
    finally
    {
        _loading = false;
    }
}
```

Toolbar split-button pattern (HTML print + RDLC download menu):

```razor
<MudButtonGroup Variant="Variant.Filled" Color="Color.Secondary" OverrideStyles="false">
    <MudButton StartIcon="@Icons.Material.Filled.Print" OnClick="PrintAsync" Disabled="@(_data is null)">
        PRINT (HTML)
    </MudButton>
    <MudMenu Icon="@Icons.Material.Filled.ArrowDropDown" Variant="Variant.Filled" Color="Color.Secondary"
             Disabled="@(_data is null || _loading)"
             AnchorOrigin="Origin.BottomRight" TransformOrigin="Origin.TopRight">
        <MudMenuItem Icon="@Icons.Material.Filled.PictureAsPdf" OnClick="@(() => DownloadPdfAsync("a4"))">
            Download PDF – A4
        </MudMenuItem>
        <MudMenuItem Icon="@Icons.Material.Filled.PictureAsPdf" OnClick="@(() => DownloadPdfAsync("longbond"))">
            Download PDF – Long Bond
        </MudMenuItem>
    </MudMenu>
</MudButtonGroup>
```

## Checklist for a New RDLC Report

1. Design `.rdlc` in VS Report Designer → place under `Modules.RdlcReporting/Reports/`.
2. Confirm `Modules.RdlcReporting.csproj` already has `<EmbeddedResource Include="Reports\*.rdlc" />` (it does).
3. Reference the owning data module's `.Contracts` project from `Modules.RdlcReporting.csproj` if not already referenced (e.g. `Modules.MyArea.Contracts`).
4. Add a vertical slice under `Modules.RdlcReporting/Features/v1/{Area}/{PrintFeature}/`:
   - `Print{Entity}Query.cs`, `Print{Entity}QueryHandler.cs`, `Print{Entity}Endpoint.cs`.
5. Create flat `internal sealed record` types whose property names match RDLC DataSet field names exactly.
6. Map the endpoint in `RdlcReportingModule.MapEndpoints` under the appropriate sub-group.
7. Add `GetReportPdfAsync` to the relevant Blazor API client targeting `api/v1/rdlc-reporting/...`.
8. Add download button / menu to the Blazor page using the data URL pattern above.

## Reference Implementation

- Helper: `src/Modules/RdlcReporting/Modules.RdlcReporting/Services/RdlcReportHelper.cs`
- Handler: `src/Modules/RdlcReporting/Modules.RdlcReporting/Features/v1/PurchaseRequests/PrintPurchaseRequest/PrintPurchaseRequestQueryHandler.cs`
- Endpoint: `src/Modules/RdlcReporting/Modules.RdlcReporting/Features/v1/PurchaseRequests/PrintPurchaseRequest/PrintPurchaseRequestEndpoint.cs`
- UI: `src/Playground/Playground.Blazor/Components/Pages/Procurement/PurchaseRequestPrintPage.razor`
- RDLC: `src/Modules/RdlcReporting/Modules.RdlcReporting/Reports/PurchaseRequest.rdlc`
