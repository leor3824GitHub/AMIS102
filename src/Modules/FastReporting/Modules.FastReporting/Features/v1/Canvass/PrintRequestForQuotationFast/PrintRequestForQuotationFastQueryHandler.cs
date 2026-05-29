using System.Data;
using System.Globalization;
using System.Reflection;
using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using AMIS.Modules.FastReporting.Services;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
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

        var org = await mediator.Send(new GetOrganizationProfileQuery(), ct).ConfigureAwait(false);

        var nf = CultureInfo.InvariantCulture;

        var headerData = new List<RfqFastHeader>
        {
            new(
                OrgName:        org?.Name ?? string.Empty,
                OrgAddress:     org?.Address ?? string.Empty,
                RivNumber:      canvass.RivNumber,
                ReturnDeadline: canvass.ReturnDeadline.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture),
                SignatoryName:  (org?.ApprovingOfficialName ?? string.Empty).ToUpperInvariant(),
                SignatoryRole:  org?.ApprovingOfficialDesignation ?? "Designation")
        };

        var lineItemsTable = BuildLineItemsTable(canvass.LineItems, query.MinRows, nf);

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
        IReadOnlyList<CanvassLineItemDto>? lineItems,
        int minRows,
        IFormatProvider nf)
    {
        var table = new DataTable("LineItemsDS") { Locale = CultureInfo.InvariantCulture };
        table.Columns.Add("CopyNumber", typeof(int));
        table.Columns.Add("Description", typeof(string));
        table.Columns.Add("Unit", typeof(string));
        table.Columns.Add("Quantity", typeof(string));

        var rows = new List<(string Desc, string Unit, string Qty)>();
        if (lineItems is not null)
            foreach (var item in lineItems.OrderBy(x => x.PrItemNo))
                rows.Add((item.Description, item.Unit, item.Quantity.ToString("N0", nf)));

        var padTo = Math.Max(minRows, rows.Count);
        while (rows.Count < padTo)
            rows.Add((string.Empty, string.Empty, string.Empty));

        for (var copy = 1; copy <= 2; copy++)
            foreach (var (desc, unit, qty) in rows)
            {
                var row = table.NewRow();
                row["CopyNumber"] = copy;
                row["Description"] = desc;
                row["Unit"] = unit;
                row["Quantity"] = qty;
                table.Rows.Add(row);
            }

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
