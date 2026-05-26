using System.Data;
using System.Globalization;
using System.Reflection;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using AMIS.Modules.FastReporting.Services;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using FastReport;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.AssetRegister.PrintSMIRFast;

public sealed class PrintSMIRFastQueryHandler(IMediator mediator)
    : IQueryHandler<PrintSMIRFastQuery, ReportFileDto>
{
    private static readonly Assembly Assembly = typeof(PrintSMIRFastQueryHandler).Assembly;
    private const string TemplateName = "SMIRFast";

    public async ValueTask<ReportFileDto> Handle(PrintSMIRFastQuery query, CancellationToken ct)
    {
        var ir = await mediator.Send(new GetIssuanceReportQuery(query.Id), ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Issuance Report '{query.Id}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), ct).ConfigureAwait(false);

        var nf = CultureInfo.InvariantCulture;
        var dateText = (ir.PostedOn ?? ir.PeriodToDate).ToString("MM/dd/yyyy", nf);

        var headerData = new List<SmirFastHeader>
        {
            new(
                OrgName:               org?.Name ?? string.Empty,
                OrgAddress:            org?.Address ?? string.Empty,
                SmirNo:                ir.ReportNo,
                FundCluster:           ir.FundCluster,
                IssuedToName:          (ir.PostedBy?.PrintedName ?? string.Empty).ToUpperInvariant(),
                Address:               ir.PostedBy?.Designation ?? string.Empty,
                SaleCheck:             string.Empty,
                TransferCheck:         string.Empty,
                DonationCheck:         string.Empty,
                OtherCheck:            string.Empty,
                Date:                  dateText,
                IssuedByName:          ir.PreparedBy.PrintedName.ToUpperInvariant(),
                IssuedByDesignation:   ir.PreparedBy.Designation ?? string.Empty,
                ApprovedByName:        (ir.CertifiedBy?.PrintedName ?? string.Empty).ToUpperInvariant(),
                ApprovedByDesignation: ir.CertifiedBy?.Designation ?? string.Empty,
                ReceivedByName:        (ir.PostedBy?.PrintedName ?? string.Empty).ToUpperInvariant())
        };

        var lineItemsTable = BuildLineItemsTable(ir, query.MinRows);

        return await FastReportService.GenerateAsync(
            Assembly,
            TemplateName,
            [
                new ReportDataSource("SmirDS", headerData),
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
            fileName: $"SMIR-{ir.ReportNo}",
            ct: ct).ConfigureAwait(false);
    }

    private static DataTable BuildLineItemsTable(PropertyIssuanceReportDto ir, int minRows)
    {
        var nf = CultureInfo.InvariantCulture;
        var table = new DataTable("LineItemsDS") { Locale = CultureInfo.InvariantCulture };
        table.Columns.Add("NameOfItem",      typeof(string));
        table.Columns.Add("Description",     typeof(string));
        table.Columns.Add("AcquisitionDate", typeof(string));
        table.Columns.Add("Quantity",        typeof(string));
        table.Columns.Add("UnitCost",        typeof(string));
        table.Columns.Add("Amount",          typeof(string));

        foreach (var line in ir.Lines.OrderBy(l => l.Snapshot.AcquisitionDate))
        {
            table.Rows.Add(
                line.Snapshot.PropertyNo,
                line.Snapshot.Description,
                line.Snapshot.AcquisitionDate.ToString("MM/dd/yyyy", nf),
                line.SnapshotQuantityIssued.ToString("N0", nf),
                line.SnapshotUnitCost.ToString("N2", nf),
                line.SnapshotAmount.ToString("N2", nf));
        }

        var padTo = Math.Max(minRows, table.Rows.Count);
        while (table.Rows.Count < padTo)
            table.Rows.Add(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        return table;
    }
}

internal sealed record SmirFastHeader(
    string OrgName,
    string OrgAddress,
    string SmirNo,
    string FundCluster,
    string IssuedToName,
    string Address,
    string SaleCheck,
    string TransferCheck,
    string DonationCheck,
    string OtherCheck,
    string Date,
    string IssuedByName,
    string IssuedByDesignation,
    string ApprovedByName,
    string ApprovedByDesignation,
    string ReceivedByName);
