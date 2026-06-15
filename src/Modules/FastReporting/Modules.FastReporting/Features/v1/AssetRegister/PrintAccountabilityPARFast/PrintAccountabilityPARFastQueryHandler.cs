using System.Data;
using System.Globalization;
using System.Reflection;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using AMIS.Modules.FastReporting.Services;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using FastReport;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.AssetRegister.PrintAccountabilityPARFast;

public sealed class PrintAccountabilityPARFastQueryHandler(IMediator mediator)
    : IQueryHandler<PrintAccountabilityPARFastQuery, ReportFileDto>
{
    private static readonly Assembly Assembly = typeof(PrintAccountabilityPARFastQueryHandler).Assembly;

    private const string TemplateName = "PropertyAcknowledgementReceiptFast";

    public async ValueTask<ReportFileDto> Handle(PrintAccountabilityPARFastQuery query, CancellationToken cancellationToken)
    {
        var accountability = await mediator.Send(new GetAccountabilityQuery(query.Id), cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Accountability document '{query.Id}' not found.");

        if (accountability.AccountabilityType != AccountabilityType.PPE_PAR)
            throw new InvalidOperationException(
                $"Document '{accountability.DocumentNo}' is an ICS, not a PAR. Use the ICS print endpoint.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        var nf = CultureInfo.InvariantCulture;

        var headerData = new List<AccountabilityParFastHeader>
        {
            new(
                EntityName:         org?.Name ?? string.Empty,
                FundCluster:        accountability.FundCluster,
                PARNo:              accountability.DocumentNo,
                PARDate:            accountability.IssuedOn.ToString("MM/dd/yyyy", nf),
                ReceivedByName:     accountability.ReceivedBy.PrintedName.ToUpperInvariant(),
                ReceivedByPosition: accountability.ReceivedBy.Designation ?? string.Empty,
                IssuedByName:       accountability.IssuedBy.PrintedName.ToUpperInvariant(),
                IssuedByPosition:   accountability.IssuedBy.Designation ?? string.Empty)
        };

        var lineItemsTable = BuildLineItemsTable(accountability, query.MinRows);

        return await FastReportService.GenerateAsync(
            Assembly,
            TemplateName,
            [
                new ReportDataSource("ParDS", headerData),
                new ReportDataSource("LineItemsDS", lineItemsTable),
            ],
            format: ReportFormat.Pdf,
            configureReport: report =>
                FastReportPaperSize.Apply(report, query.PaperSize, query.Orientation),
            configureDataBindings: report =>
            {
                if (report.FindObject("Data1") is DataBand dataBand)
                    dataBand.DataSource = report.GetDataSource("LineItemsDS");
            },
            fileName: $"PAR-{accountability.DocumentNo}",
            ct: cancellationToken).ConfigureAwait(false);
    }

    // DataTable uses same column names as PropertyAcknowledgementReceiptFast.frx.
    private static DataTable BuildLineItemsTable(PropertyAccountabilityDto accountability, int minRows)
    {
        var nf = CultureInfo.InvariantCulture;
        var table = new DataTable("LineItemsDS") { Locale = CultureInfo.InvariantCulture };
        table.Columns.Add("Quantity",     typeof(string));
        table.Columns.Add("Unit",         typeof(string));
        table.Columns.Add("Description",  typeof(string));
        table.Columns.Add("PropertyNo",   typeof(string));
        table.Columns.Add("DateAcquired", typeof(string));
        table.Columns.Add("Amount",       typeof(string));

        foreach (var line in accountability.Lines.OrderBy(l => l.SnapshotItemNo))
        {
            var snap = line.Snapshot;
            var total = snap.UnitCost * line.IssuedQty;

            table.Rows.Add(
                line.IssuedQty.ToString("N0", nf),
                snap.Unit,
                snap.Description,
                snap.PropertyNo,
                snap.AcquisitionDate.ToString("MM/dd/yyyy", nf),
                total.ToString("N2", nf));
        }

        var padTo = Math.Max(minRows, table.Rows.Count);
        while (table.Rows.Count < padTo)
            table.Rows.Add(
                string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty);

        return table;
    }
}

internal sealed record AccountabilityParFastHeader(
    string EntityName,
    string FundCluster,
    string PARNo,
    string PARDate,
    string ReceivedByName,
    string ReceivedByPosition,
    string IssuedByName,
    string IssuedByPosition);
