using System.Data;
using System.Globalization;
using System.Reflection;
using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using AMIS.Modules.FastReporting.Services;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using FastReport;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.PurchaseRequests.PrintPurchaseRequestFast;

public sealed class PrintPurchaseRequestFastQueryHandler(IMediator mediator)
    : IQueryHandler<PrintPurchaseRequestFastQuery, ReportFileDto>
{
    private static readonly Assembly Assembly = typeof(PrintPurchaseRequestFastQueryHandler).Assembly;
    private const string LandscapeTemplate = "PurchaseRequestFast";
    private const string PortraitTemplate = "PurchaseRequestPortraitFast";

    public async ValueTask<ReportFileDto> Handle(PrintPurchaseRequestFastQuery query, CancellationToken ct)
    {
        var pr = await mediator.Send(new GetPurchaseRequestQuery(query.Id), ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Purchase request '{query.Id}' not found.");

        var org                    = await mediator.Send(new GetOrganizationProfileQuery(), ct).ConfigureAwait(false);
        var requestedByDesignation = await ResolveDesignationAsync(pr.CreatedBy, ct).ConfigureAwait(false);

        var headerData = new List<PrFastHeader>
        {
            new(
                OrgName:                  org?.Name ?? string.Empty,
                OrgShortName:             org?.ShortName ?? string.Empty,
                OrgAddress:               org?.Address ?? string.Empty,
                PrNumber:                 pr.PrNumber,
                PrDate:                   pr.PrDate.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture),
                DepartmentName:           pr.DepartmentName,
                ResponsibilityCenterCode: pr.ResponsibilityCenterCode ?? string.Empty,
                Purpose:                  pr.Purpose ?? string.Empty,
                RequestedByName:          (pr.RequestedByName ?? string.Empty).ToUpperInvariant(),
                RequestedByDesignation:   requestedByDesignation,
                ApprovedByName:           (pr.ApprovedByName ?? org?.RegionalManagerName ?? string.Empty).ToUpperInvariant(),
                ApprovedByDesignation:    org?.RegionalManagerDesignation ?? "Regional Manager II")
        };

        var lineItemsTable = BuildLineItemsTable(pr, query.MinRows);
        var lineItemsForPortrait = pr.LineItems.OrderBy(x => x.ItemNo).ToList();

        // Landscape lays out two copies side-by-side using a DataBand. Portrait stacks two
        // copies top/bottom — DataBand can't flow into two non-contiguous page regions, so
        // the portrait template uses 12 fixed row slots per copy populated by name below.
        var templateName = FastReportPaperSize.IsLandscape(query.Orientation)
            ? LandscapeTemplate
            : PortraitTemplate;

        return await FastReportService.GenerateAsync(
            Assembly,
            templateName,
            [
                new ReportDataSource("PurchaseRequestDS", headerData),
                new ReportDataSource("LineItemsDS", lineItemsTable),
            ],
            format: ReportFormat.Pdf,
            configureReport: report =>
            {
                FastReportPaperSize.Apply(report, query.PaperSize, query.Orientation);
                if (query.Copies == 1)
                    HideSecondCopy(report);
            },
            configureDataBindings: report =>
            {
                // Landscape uses a DataBand; bind LineItemsDS explicitly because the FRX-based
                // DataSource name lookup doesn't always re-resolve to the runtime-registered source.
                if (report.FindObject("Data1") is DataBand dataBand)
                    dataBand.DataSource = report.GetDataSource("LineItemsDS");

                // Portrait template has static T_R{i}_* / B_R{i}_* row slots — populate them
                // from the ordered line items list. No-op for templates without those slots.
                PopulatePortraitStaticRows(report, lineItemsForPortrait);
            },
            fileName: $"PR-{pr.PrNumber}",
            ct: ct).ConfigureAwait(false);
    }

    // Use a DataTable (not List<record>) for line items: BusinessObjectDataSource in
    // FastReport.OpenSource only emits the first row of a padded list; TableDataSource backed
    // by DataTable iterates correctly and is the canonical path for variable-row tabular data.
    private static DataTable BuildLineItemsTable(PurchaseRequestDto pr, int minRows)
    {
        var nf = CultureInfo.InvariantCulture;
        var table = new DataTable("LineItemsDS") { Locale = CultureInfo.InvariantCulture };
        table.Columns.Add("UnitOfIssue", typeof(string));
        table.Columns.Add("ItemDescription", typeof(string));
        table.Columns.Add("Quantity", typeof(string));
        table.Columns.Add("EstimatedUnitCost", typeof(string));
        table.Columns.Add("EstimatedTotalCost", typeof(string));

        foreach (var li in pr.LineItems.OrderBy(x => x.ItemNo))
        {
            table.Rows.Add(
                li.UnitOfIssue,
                li.ItemDescription,
                li.Quantity.ToString("N2", nf),
                li.EstimatedUnitCost.ToString("N2", nf),
                li.EstimatedTotalCost.ToString("N2", nf));
        }

        // FastReport.OpenSource has no CompleteToNRows; pad with blank rows so the data band
        // always renders at least MinRows rows.
        var padTo = Math.Max(minRows, table.Rows.Count);
        while (table.Rows.Count < padTo)
            table.Rows.Add(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        return table;
    }

    private async ValueTask<string> ResolveDesignationAsync(string? identityUserId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(identityUserId))
            return string.Empty;

        var employee = await mediator.Send(
            new GetEmployeeReferenceByIdentityUserIdQuery(identityUserId), ct).ConfigureAwait(false);

        return employee?.PositionName ?? string.Empty;
    }

    // Hide the second copy: "R_*" (right copy in landscape), "B_*" (bottom copy in portrait),
    // and the portrait cut-line indicator, which is meaningless without a second copy.
    private static void HideSecondCopy(Report report)
    {
        foreach (var obj in report.AllObjects.OfType<ReportComponentBase>())
        {
            if (obj.Name is not { Length: > 1 } n)
                continue;

            if (n.StartsWith("R_", StringComparison.Ordinal)
                || n.StartsWith("B_", StringComparison.Ordinal)
                || string.Equals(n, "CutLine", StringComparison.Ordinal))
            {
                obj.Visible = false;
            }
        }
    }

    // Portrait template has 12 fixed row slots per copy (T_R0..T_R11, B_R0..B_R11). Set each
    // field by name; slots beyond the actual item count stay blank (template default Text="").
    private const int PortraitRowSlots = 12;

    private static void PopulatePortraitStaticRows(
        Report report,
        List<PurchaseRequestLineItemDto> items)
    {
        var nf = CultureInfo.InvariantCulture;
        var count = Math.Min(items.Count, PortraitRowSlots);

        for (int i = 0; i < count; i++)
        {
            var li = items[i];
            var unit       = li.UnitOfIssue;
            var desc       = li.ItemDescription;
            var qty        = li.Quantity.ToString("N2", nf);
            var unitCost   = li.EstimatedUnitCost.ToString("N2", nf);
            var totalCost  = li.EstimatedTotalCost.ToString("N2", nf);

            SetText(report, $"T_R{i}_Unit",      unit);
            SetText(report, $"T_R{i}_Desc",      desc);
            SetText(report, $"T_R{i}_Qty",       qty);
            SetText(report, $"T_R{i}_UnitCost",  unitCost);
            SetText(report, $"T_R{i}_TotalCost", totalCost);

            SetText(report, $"B_R{i}_Unit",      unit);
            SetText(report, $"B_R{i}_Desc",      desc);
            SetText(report, $"B_R{i}_Qty",       qty);
            SetText(report, $"B_R{i}_UnitCost",  unitCost);
            SetText(report, $"B_R{i}_TotalCost", totalCost);
        }
    }

    private static void SetText(Report report, string name, string value)
    {
        if (report.FindObject(name) is TextObject t)
            t.Text = value;
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
