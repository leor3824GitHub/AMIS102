---
paths:
  - "src/**/Reports/**/*.rdlc"
  - "src/**/Features/**/Print*"
  - "src/BuildingBlocks/Reporting/**"
---

# RDLC Reports with ReportViewerCore

AMIS uses **ReportViewerCore.NETCore** (v15.1.33) for server-side RDLC → PDF rendering. The entry point is `RdlcReportHelper.Render()` in `BuildingBlocks/Reporting`.

## Package

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="ReportViewerCore.NETCore" Version="15.1.33" />

<!-- Your module's .csproj -->
<PackageReference Include="ReportViewerCore.NETCore" />
```

Exposes `Microsoft.Reporting.NETCore` namespace — `LocalReport`, `ReportDataSource`, etc. Same API as the official package.

## RdlcReportHelper

`src/BuildingBlocks/Reporting/RdlcReportHelper.cs` — static utility, three steps internally:

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

## Embed the .rdlc File

In the module `.csproj`:

```xml
<EmbeddedResource Include="Reports\*.rdlc" />
```

Resource name pattern: `{RootNamespace}.Reports.{FileName}.rdlc`

Example: `AMIS.Modules.ProcurementAcquisition.Reports.PurchaseRequest.rdlc`

## Handler Pattern

Reference: `PrintPurchaseRequestQueryHandler.cs`

```csharp
public sealed class MyReportQueryHandler(...) : IQueryHandler<MyReportQuery, byte[]>
{
    private static readonly Assembly Assembly = typeof(MyReportQueryHandler).Assembly;
    private const string ReportResource = "My.Namespace.Reports.MyReport.rdlc";

    public async ValueTask<byte[]> Handle(MyReportQuery query, CancellationToken ct)
    {
        // 1. Fetch data
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

```csharp
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
.WithName("Module_PrintMyReport")
.WithSummary("Generate PDF for my report")
.Produces(200, null, "application/pdf")
.RequirePermission(MyPermissions.MyEntity.View);
```

## Paper Size Shortcuts

`GovernmentPaperSizes` in BuildingBlocks defines preset `(width, height)` tuples:

| Shorthand | Width | Height |
|-----------|-------|--------|
| `a4` | 21cm | 29.7cm |
| `legal` | 8.5in | 14in |
| `longbond` | 8.5in | 13in |
| `halfa4` | 14.85cm | 21cm |
| `halflegal` | 7in | 8.5in |
| `halflong` | 6.5in | 8.5in |

## Blazor UI Integration

Fetch bytes from the API client, open as data URL in a new tab:

```csharp
// In API client
public async Task<byte[]> GetReportPdfAsync(Guid id, string? paperSize = null, CancellationToken ct = default)
{
    var url = string.IsNullOrWhiteSpace(paperSize)
        ? $"{Base}/{id}/print"
        : $"{Base}/{id}/print?pageWidth={paperSize}";
    using var r = await http.GetAsync(url, ct);
    r.EnsureSuccessStatusCode();
    return await r.Content.ReadAsByteArrayAsync(ct);
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

1. Design `.rdlc` in VS Report Designer → place under `Reports/` in the module
2. Add `<EmbeddedResource Include="Reports\*.rdlc" />` to the module `.csproj`
3. Add `<PackageReference Include="ReportViewerCore.NETCore" />` to the module `.csproj` (or reference `BuildingBlocks/Reporting`)
4. Create flat `internal sealed record` types whose property names match RDLC DataSet field names exactly
5. Implement an `IQueryHandler<TQuery, byte[]>` that calls `RdlcReportHelper.Render(...)`
6. Add a `MapGet` endpoint that dispatches the query and returns `Results.File(bytes, "application/pdf", "name.pdf")`
7. Add `GetReportPdfAsync` to the Blazor API client
8. Add download button / menu to the Blazor page using the data URL pattern above

## Reference Implementation

- Helper: `src/BuildingBlocks/Reporting/RdlcReportHelper.cs`
- Handler: `src/Modules/ProcurementAcquisition/.../PrintPurchaseRequest/PrintPurchaseRequestQueryHandler.cs`
- Endpoint: `src/Modules/ProcurementAcquisition/.../PrintPurchaseRequest/PrintPurchaseRequestEndpoint.cs`
- UI: `src/Playground/Playground.Blazor/Components/Pages/Procurement/PurchaseRequestPrintPage.razor`
- RDLC: `src/Modules/ProcurementAcquisition/.../Reports/PurchaseRequest.rdlc`
