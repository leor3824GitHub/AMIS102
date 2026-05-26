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
    private const string TemplateName = "PurchaseRequestFast";

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

        return await FastReportService.GenerateAsync(
            Assembly,
            TemplateName,
            [
                new ReportDataSource("PurchaseRequestDS", headerData),
                new ReportDataSource("LineItemsDS", lineItemsTable),
            ],
            format: ReportFormat.Pdf,
            configureReport: report =>
            {
                FastReportPaperSize.Apply(report, query.PaperSize, query.Orientation);
                if (query.Copies == 1 || !FastReportPaperSize.IsLandscape(query.Orientation))
                    HideRightCopy(report);
            },
            configureDataBindings: report =>
            {
                // FRX-based DataSource="LineItemsDS" reference is a name lookup that doesn't
                // always re-resolve to a runtime-registered TableDataSource. Bind it explicitly.
                if (report.FindObject("Data1") is DataBand dataBand)
                    dataBand.DataSource = report.GetDataSource("LineItemsDS");
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
