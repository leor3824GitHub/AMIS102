using System.Data;
using System.Globalization;
using System.Reflection;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using FastReport;
using FastReport.Export.PdfSimple;
using Mediator;

namespace AMIS.Modules.Reporting.Features.v1.PurchaseRequests.PrintPurchaseRequestFast;

public sealed class PrintPurchaseRequestFastQueryHandler(IMediator mediator)
    : IQueryHandler<PrintPurchaseRequestFastQuery, byte[]>
{
    private static readonly Assembly Assembly = typeof(PrintPurchaseRequestFastQueryHandler).Assembly;
    private const string TemplateResource = "AMIS.Modules.Reporting.Templates.PurchaseRequestFast.frx";

    public async ValueTask<byte[]> Handle(PrintPurchaseRequestFastQuery query, CancellationToken ct)
    {
        var pr = await mediator.Send(new GetPurchaseRequestQuery(query.Id), ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Purchase request '{query.Id}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), ct).ConfigureAwait(false);

        var requestedByDesignation = await ResolveDesignationByNameAsync(pr.RequestedByName, ct).ConfigureAwait(false);

        var headerData = new List<PrFastHeader>
        {
            new(
                OrgName:                  org?.Name ?? string.Empty,
                OrgShortName:             org?.ShortName ?? string.Empty,
                OrgAddress:               org?.Address ?? string.Empty,
                PrNumber:                 pr.PrNumber,
                PrDate:                   pr.PrDate.ToString("MM/dd/yyyy"),
                DepartmentName:           pr.DepartmentName,
                ResponsibilityCenterCode: pr.ResponsibilityCenterCode ?? string.Empty,
                Purpose:                  pr.Purpose ?? string.Empty,
                RequestedByName:          pr.RequestedByName ?? string.Empty,
                RequestedByDesignation:   requestedByDesignation,
                ApprovedByName:           pr.ApprovedByName ?? org?.RegionalManagerName ?? string.Empty,
                ApprovedByDesignation:    org?.RegionalManagerDesignation ?? "Regional Manager II")
        };

        var nf = CultureInfo.InvariantCulture;

        // Use a DataTable for line items (not a List<record>). BusinessObjectDataSource
        // in FastReport.OpenSource was only emitting the first row of the padded list;
        // a TableDataSource backed by DataTable iterates correctly and is the canonical
        // path for variable-row tabular data.
        var lineItemsTable = new DataTable("LineItemsDS");
        lineItemsTable.Columns.Add("UnitOfIssue", typeof(string));
        lineItemsTable.Columns.Add("ItemDescription", typeof(string));
        lineItemsTable.Columns.Add("Quantity", typeof(string));
        lineItemsTable.Columns.Add("EstimatedUnitCost", typeof(string));
        lineItemsTable.Columns.Add("EstimatedTotalCost", typeof(string));

        foreach (var li in pr.LineItems.OrderBy(x => x.ItemNo))
        {
            lineItemsTable.Rows.Add(
                li.UnitOfIssue,
                li.ItemDescription,
                li.Quantity.ToString("N2", nf),
                li.EstimatedUnitCost.ToString("N2", nf),
                li.EstimatedTotalCost.ToString("N2", nf));
        }

        // FastReport.OpenSource has no CompleteToNRows; pad the table with empty rows
        // so the line-item table always renders MinRows rows regardless of item count.
        var padTo = Math.Max(query.MinRows, lineItemsTable.Rows.Count);
        while (lineItemsTable.Rows.Count < padTo)
            lineItemsTable.Rows.Add(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        ct.ThrowIfCancellationRequested();

        using var stream = Assembly.GetManifestResourceStream(TemplateResource)
            ?? throw new InvalidOperationException(
                $"FastReport template not found: '{TemplateResource}'. " +
                "Ensure the .frx file is in Templates/ with Build Action = EmbeddedResource.");

        using var report = new Report();
        report.Load(stream);

        ApplyPaperSize(report, query.PaperSize, query.Orientation);
        if (query.Copies == 1 || string.Equals(query.Orientation, "portrait", StringComparison.OrdinalIgnoreCase))
            HideRightCopy(report);

        report.RegisterData(headerData, "PurchaseRequestDS", maxNestingLevel: 2);
        report.RegisterData(lineItemsTable, "LineItemsDS");

        // PurchaseRequestDS is not the DataSource of any band — it's only referenced
        // from TextObjects on the title/summary bands. Business object data sources are
        // disabled by default; enable them so [PurchaseRequestDS.X] expressions resolve.
        report.GetDataSource("PurchaseRequestDS").Enabled = true;
        report.GetDataSource("LineItemsDS").Enabled = true;

        // FRX-based DataSource="LineItemsDS" reference is a name lookup that doesn't
        // always re-resolve to a runtime-registered TableDataSource. Bind it explicitly.
        if (report.FindObject("Data1") is DataBand dataBand)
            dataBand.DataSource = report.GetDataSource("LineItemsDS");

        await report.PrepareAsync(ct).ConfigureAwait(false);

        using var output = new MemoryStream();
        using var export = new PDFSimpleExport();
        export.Export(report, output);
        return output.ToArray();
    }

    // Paper sizes in mm, landscape orientation (width × height).
    // The .frx body is fixed at 297×210 (A4 landscape) — larger papers get extra whitespace
    // on the right; smaller would clip, so don't allow paper narrower than A4.
    private static (float WidthMm, float HeightMm) ResolvePaper(string paperSize) => paperSize switch
    {
        "legal"    => (355.6f, 215.9f), // 14 × 8.5 in
        "longbond" => (330.2f, 215.9f), // 13 × 8.5 in
        "letter"   => (279.4f, 215.9f), // 11 × 8.5 in  — narrower than A4; will clip slightly
        _          => (297f,   210f),   // a4 (default)
    };

    private static void ApplyPaperSize(Report report, string paperSize, string orientation)
    {
        var (longSide, shortSide) = ResolvePaper(paperSize);
        var landscape = !string.Equals(orientation, "portrait", StringComparison.OrdinalIgnoreCase);

        foreach (var page in report.Pages.OfType<ReportPage>())
        {
            // Order matters: Landscape setter swaps Width/Height if its value changes.
            // Set Landscape FIRST, then assign dimensions in the resulting orientation.
            page.Landscape = landscape;
            if (landscape)
            {
                page.PaperWidth = longSide;
                page.PaperHeight = shortSide;
            }
            else
            {
                page.PaperWidth = shortSide;
                page.PaperHeight = longSide;
            }
        }
    }

    // Look up the requester's position name (used as the printed Designation).
    // The PR stores only a name string, not an employee FK, so we match by trimmed full name.
    // Returns "" when ambiguous or not found — the field is informational, not load-bearing.
    private async ValueTask<string> ResolveDesignationByNameAsync(string? fullName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return string.Empty;

        var trimmed = fullName.Trim();
        var page = await mediator.Send(
            new SearchEmployeeReferencesQuery { Keyword = trimmed, PageSize = 25, IsActive = true },
            ct).ConfigureAwait(false);

        var match = page.Items?.FirstOrDefault(e =>
            string.Equals($"{e.FirstName} {e.LastName}".Trim(), trimmed, StringComparison.OrdinalIgnoreCase));

        return match?.PositionName ?? string.Empty;
    }

    // Hide every object whose Name starts with "R_" — the right-copy elements in the .frx.
    private static void HideRightCopy(Report report)
    {
        foreach (var obj in report.AllObjects.OfType<ReportComponentBase>())
        {
            if (obj.Name is { Length: > 1 } n && n.StartsWith("R_", StringComparison.Ordinal))
                obj.Visible = false;
        }
    }
}

internal sealed record PrFastHeader(
    string OrgName,
    string OrgShortName,
    string OrgAddress,
    string PrNumber,
    string PrDate,
    string DepartmentName,
    string ResponsibilityCenterCode,
    string Purpose,
    string RequestedByName,
    string RequestedByDesignation,
    string ApprovedByName,
    string ApprovedByDesignation);
