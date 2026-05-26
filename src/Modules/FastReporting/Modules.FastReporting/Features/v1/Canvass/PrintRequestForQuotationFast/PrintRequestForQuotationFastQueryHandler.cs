using System.Data;
using System.Globalization;
using System.Reflection;
using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using AMIS.Modules.FastReporting.Services;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using FastReport;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.Canvass.PrintRequestForQuotationFast;

public sealed class PrintRequestForQuotationFastQueryHandler(IMediator mediator)
    : IQueryHandler<PrintRequestForQuotationFastQuery, ReportFileDto>
{
    private static readonly Assembly Assembly = typeof(PrintRequestForQuotationFastQueryHandler).Assembly;
    private const string TemplateName = "RequestForQuotationFast";

    public async ValueTask<ReportFileDto> Handle(PrintRequestForQuotationFastQuery query, CancellationToken ct)
    {
        var canvass = await mediator.Send(new GetCanvassRequestQuery(query.Id), ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Canvass request '{query.Id}' not found.");

        var pr = await mediator.Send(new GetPurchaseRequestQuery(canvass.PurchaseRequestId), ct).ConfigureAwait(false);
        var org = await mediator.Send(new GetOrganizationProfileQuery(), ct).ConfigureAwait(false);

        var nf = CultureInfo.InvariantCulture;

        var headerData = new List<RfqFastHeader>
        {
            new(
                OrgName:        org?.Name ?? string.Empty,
                OrgAddress:     org?.Address ?? string.Empty,
                RivNumber:      canvass.RivNumber,
                ReturnDeadline: canvass.ReturnDeadline.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture),
                SignatoryName:  (org?.RegionalManagerName ?? string.Empty).ToUpperInvariant(),
                SignatoryRole:  org?.RegionalManagerDesignation ?? "Regional Manager II")
        };

        var lineItemsTable = BuildLineItemsTable(pr?.LineItems, query.MinRows, nf);

        return await FastReportService.GenerateAsync(
            Assembly,
            TemplateName,
            [
                new ReportDataSource("RfqDS", headerData),
                new ReportDataSource("LineItemsDS", lineItemsTable),
            ],
            format: ReportFormat.Pdf,
            configureReport: report =>
            {
                FastReportPaperSize.Apply(report, query.PaperSize, query.Orientation);
            },
            configureDataBindings: report =>
            {
                if (report.FindObject("Data1") is DataBand dataBand)
                    dataBand.DataSource = report.GetDataSource("LineItemsDS");
            },
            fileName: $"RFQ-{canvass.RivNumber}",
            ct: ct).ConfigureAwait(false);
    }

    private static DataTable BuildLineItemsTable(
        IReadOnlyList<PurchaseRequestLineItemDto>? lineItems,
        int minRows,
        IFormatProvider nf)
    {
        var table = new DataTable("LineItemsDS") { Locale = CultureInfo.InvariantCulture };
        table.Columns.Add("Description", typeof(string));
        table.Columns.Add("Unit", typeof(string));
        table.Columns.Add("Quantity", typeof(string));

        if (lineItems is not null)
        {
            foreach (var item in lineItems.OrderBy(x => x.ItemNo))
            {
                var row = table.NewRow();
                row["Description"] = item.ItemDescription;
                row["Unit"] = item.UnitOfIssue;
                row["Quantity"] = item.Quantity.ToString("N0", nf);
                table.Rows.Add(row);
            }
        }

        var padTo = Math.Max(minRows, table.Rows.Count);
        while (table.Rows.Count < padTo)
            table.Rows.Add(string.Empty, string.Empty, string.Empty);

        return table;
    }
}

internal sealed record RfqFastHeader(
    string OrgName,
    string OrgAddress,
    string RivNumber,
    string ReturnDeadline,
    string SignatoryName,
    string SignatoryRole);
