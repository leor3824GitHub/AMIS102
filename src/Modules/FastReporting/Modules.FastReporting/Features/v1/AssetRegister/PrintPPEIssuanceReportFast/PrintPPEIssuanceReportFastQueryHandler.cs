using System.Data;
using System.Globalization;
using System.Reflection;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using AMIS.Modules.FastReporting.Services;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using FastReport;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.AssetRegister.PrintPPEIssuanceReportFast;

public sealed class PrintPPEIssuanceReportFastQueryHandler(IMediator mediator)
    : IQueryHandler<PrintPPEIssuanceReportFastQuery, ReportFileDto>
{
    private static readonly Assembly Assembly = typeof(PrintPPEIssuanceReportFastQueryHandler).Assembly;
    private const string TemplateName = "PPEIssuanceReportFast";

    public async ValueTask<ReportFileDto> Handle(PrintPPEIssuanceReportFastQuery query, CancellationToken cancellationToken)
    {
        var ir = await mediator.Send(new GetIssuanceReportQuery(query.Id), cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Issuance Report '{query.Id}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        var nf = CultureInfo.InvariantCulture;
        var dateText = ir.Date.ToString("MM/dd/yyyy", nf);

        var headerData = new List<IrFastHeader>
        {
            new(
                OrgName:               org?.Name ?? string.Empty,
                OrgAddress:            org?.Address ?? string.Empty,
                IrNo:                  ir.ReportNo,
                Date:                  dateText,
                FundCluster:           ir.FundCluster,
                IssuedToName:          ir.IssuedTo.PrintedName.ToUpperInvariant(),
                IssuedToDesignation:   ir.IssuedTo.Designation ?? string.Empty,
                IssuedByName:          ir.IssuedBy.PrintedName.ToUpperInvariant(),
                IssuedByDesignation:   ir.IssuedBy.Designation ?? string.Empty,
                ApprovedByName:        ir.ApprovedBy.PrintedName.ToUpperInvariant(),
                ApprovedByDesignation: ir.ApprovedBy.Designation ?? string.Empty)
        };

        var lineItemsTable = BuildLineItemsTable(ir, query.MinRows);

        return await FastReportService.GenerateAsync(
            Assembly,
            TemplateName,
            [
                new ReportDataSource("IrDS", headerData),
                new ReportDataSource("LineItemsDS", lineItemsTable),
            ],
            format: ReportFormat.Pdf,
            configureReport: report =>
            {
                FastReportPaperSize.Apply(report, query.PaperSize, query.Orientation);
                if (query.DataOnly)
                    FastReportOverlay.ApplyDataOnly(
                        report,
                        dataSourceNames: ["IrDS", "LineItemsDS"],
                        suppressFields: ["OrgName", "OrgAddress"],
                        offsetXmm: query.OffsetXmm,
                        offsetYmm: query.OffsetYmm);
            },
            configureDataBindings: report =>
            {
                if (report.FindObject("Data1") is DataBand dataBand)
                    dataBand.DataSource = report.GetDataSource("LineItemsDS");
            },
            fileName: $"PPEIR-{ir.ReportNo}",
            ct: cancellationToken).ConfigureAwait(false);
    }

    // DataTable (not List<record>) for line items — FastReport's TableDataSource iterates
    // DataTable rows correctly; BusinessObjectDataSource only emits the first row of a padded list.
    private static DataTable BuildLineItemsTable(PropertyIssuanceReportDto ir, int minRows)
    {
        var nf = CultureInfo.InvariantCulture;
        var table = new DataTable("LineItemsDS") { Locale = CultureInfo.InvariantCulture };
        table.Columns.Add("PropertyCode",        typeof(string));
        table.Columns.Add("SerialNumber",        typeof(string));
        table.Columns.Add("Specifications",      typeof(string));
        table.Columns.Add("AcquisitionDate",     typeof(string));
        table.Columns.Add("AcquisitionCost",     typeof(string));
        table.Columns.Add("AccumDepreciation",   typeof(string));
        table.Columns.Add("BookValue",           typeof(string));

        foreach (var line in ir.Lines.OrderBy(l => l.ItemNo))
        {
            table.Rows.Add(
                line.Snapshot.PropertyNo,
                line.Snapshot.SerialNo ?? string.Empty,
                line.Snapshot.Description,
                line.Snapshot.AcquisitionDate.ToString("MM/dd/yyyy", nf),
                line.SnapshotUnitCost.ToString("N2", nf),
                line.AccumulatedDepreciation?.ToString("N2", nf) ?? string.Empty,
                line.BookValue?.ToString("N2", nf) ?? line.SnapshotAmount.ToString("N2", nf));
        }

        var padTo = Math.Max(minRows, table.Rows.Count);
        while (table.Rows.Count < padTo)
            table.Rows.Add(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        return table;
    }
}

internal sealed record IrFastHeader(
    string OrgName,
    string OrgAddress,
    string IrNo,
    string Date,
    string FundCluster,
    string IssuedToName,
    string IssuedToDesignation,
    string IssuedByName,
    string IssuedByDesignation,
    string ApprovedByName,
    string ApprovedByDesignation);
