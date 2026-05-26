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

namespace AMIS.Modules.FastReporting.Features.v1.AssetRegister.PrintAccountabilityICSFast;

public sealed class PrintAccountabilityICSFastQueryHandler(IMediator mediator)
    : IQueryHandler<PrintAccountabilityICSFastQuery, ReportFileDto>
{
    private static readonly Assembly Assembly = typeof(PrintAccountabilityICSFastQueryHandler).Assembly;

    // Reuses the same layout as the AssetManagement ICS — field names are identical.
    private const string TemplateName = "InventoryCustodianSlipFast";

    public async ValueTask<ReportFileDto> Handle(PrintAccountabilityICSFastQuery query, CancellationToken ct)
    {
        var accountability = await mediator.Send(new GetAccountabilityQuery(query.Id), ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Accountability document '{query.Id}' not found.");

        if (accountability.AccountabilityType != AccountabilityType.SE_ICS)
            throw new InvalidOperationException(
                $"Document '{accountability.DocumentNo}' is a PAR, not an ICS. Use the PAR print endpoint.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), ct).ConfigureAwait(false);

        var nf = CultureInfo.InvariantCulture;

        var headerData = new List<AccountabilityIcsFastHeader>
        {
            new(
                EntityName:         org?.Name ?? string.Empty,
                FundCluster:        accountability.FundCluster,
                ICSNo:              accountability.DocumentNo,
                ICSDate:            accountability.IssuedOn.ToString("MM/dd/yyyy", nf),
                IssuedFromName:     accountability.IssuedBy.PrintedName.ToUpperInvariant(),
                IssuedFromPosition: accountability.IssuedBy.Designation ?? string.Empty,
                IssuedFromOffice:   string.Empty,
                ReceivedByName:     accountability.ReceivedBy.PrintedName.ToUpperInvariant(),
                ReceivedByPosition: accountability.ReceivedBy.Designation ?? string.Empty,
                ReceivedByOffice:   string.Empty)
        };

        var lineItemsTable = BuildLineItemsTable(accountability, query.MinRows);

        return await FastReportService.GenerateAsync(
            Assembly,
            TemplateName,
            [
                new ReportDataSource("IcsDS", headerData),
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
            fileName: $"ICS-{accountability.DocumentNo}",
            ct: ct).ConfigureAwait(false);
    }

    // DataTable uses same column names as InventoryCustodianSlipFast.frx.
    private static DataTable BuildLineItemsTable(PropertyAccountabilityDto accountability, int minRows)
    {
        var nf = CultureInfo.InvariantCulture;
        var table = new DataTable("LineItemsDS") { Locale = CultureInfo.InvariantCulture };
        table.Columns.Add("Quantity",        typeof(string));
        table.Columns.Add("Unit",            typeof(string));
        table.Columns.Add("UnitCost",        typeof(string));
        table.Columns.Add("TotalCost",       typeof(string));
        table.Columns.Add("Description",     typeof(string));
        table.Columns.Add("InventoryItemNo", typeof(string));
        table.Columns.Add("EstUsefulLife",   typeof(string));

        foreach (var line in accountability.Lines.OrderBy(l => l.SnapshotItemNo))
        {
            var snap = line.Snapshot;
            var total = snap.UnitCost * line.IssuedQty;
            var eul = $"{snap.EstimatedUsefulLifeYears} yr(s)";

            table.Rows.Add(
                line.IssuedQty.ToString("N0", nf),
                snap.Unit,
                snap.UnitCost.ToString("N2", nf),
                total.ToString("N2", nf),
                snap.Description,
                snap.PropertyNo,
                eul);
        }

        var padTo = Math.Max(minRows, table.Rows.Count);
        while (table.Rows.Count < padTo)
            table.Rows.Add(
                string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty);

        return table;
    }
}

internal sealed record AccountabilityIcsFastHeader(
    string EntityName,
    string FundCluster,
    string ICSNo,
    string ICSDate,
    string IssuedFromName,
    string IssuedFromPosition,
    string IssuedFromOffice,
    string ReceivedByName,
    string ReceivedByPosition,
    string ReceivedByOffice);
