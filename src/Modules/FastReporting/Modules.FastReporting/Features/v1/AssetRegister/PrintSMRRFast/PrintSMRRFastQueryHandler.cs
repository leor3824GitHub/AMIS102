using System.Data;
using System.Globalization;
using System.Reflection;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using AMIS.Modules.FastReporting.Services;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using FastReport;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.AssetRegister.PrintSMRRFast;

public sealed class PrintSMRRFastQueryHandler(IMediator mediator)
    : IQueryHandler<PrintSMRRFastQuery, ReportFileDto>
{
    private static readonly Assembly Assembly = typeof(PrintSMRRFastQueryHandler).Assembly;
    private const string TemplateName = "SMRRFast";

    public async ValueTask<ReportFileDto> Handle(PrintSMRRFastQuery query, CancellationToken cancellationToken)
    {
        var rr = await mediator.Send(new GetReceivingReportQuery(query.Id), cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Receiving Report '{query.Id}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        var nf = CultureInfo.InvariantCulture;

        var headerData = new List<SmrrFastHeader>
        {
            new(
                OrgName:                  org?.Name ?? string.Empty,
                OrgAddress:               org?.Address ?? string.Empty,
                RrNo:                     rr.ReportNo,
                Date:                     rr.Date.ToString("MM/dd/yyyy", nf),
                ReceivedFrom:             rr.ReceivedFrom,
                Address:                  rr.Address ?? string.Empty,
                PurchaseCheck:            rr.ReceiptType == ReceiptType.Purchase  ? "X" : string.Empty,
                TransferCheck:            rr.ReceiptType == ReceiptType.Transfer  ? "X" : string.Empty,
                DonationCheck:            rr.ReceiptType == ReceiptType.Donation  ? "X" : string.Empty,
                OtherCheck:               rr.ReceiptType == ReceiptType.Other     ? "X" : string.Empty,
                ReceivedByName:           rr.ReceivedBy.PrintedName.ToUpperInvariant(),
                ReceivedByDesignation:    rr.ReceivedBy.Designation ?? string.Empty,
                NotedByName:              (rr.NotedBy?.PrintedName ?? string.Empty).ToUpperInvariant(),
                NotedByDesignation:       rr.NotedBy?.Designation ?? string.Empty)
        };

        var lineItemsTable = BuildLineItemsTable(rr, query.MinRows);

        return await FastReportService.GenerateAsync(
            Assembly,
            TemplateName,
            [
                new ReportDataSource("RrDS", headerData),
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
            fileName: $"SMRR-{rr.ReportNo}",
            ct: cancellationToken).ConfigureAwait(false);
    }

    private static DataTable BuildLineItemsTable(ReceivingReportDto rr, int minRows)
    {
        var nf = CultureInfo.InvariantCulture;
        var table = new DataTable("LineItemsDS") { Locale = CultureInfo.InvariantCulture };
        table.Columns.Add("Reference",       typeof(string));
        table.Columns.Add("NameOfItem",      typeof(string));
        table.Columns.Add("Description",     typeof(string));
        table.Columns.Add("AcquisitionDate", typeof(string));
        table.Columns.Add("Quantity",        typeof(string));
        table.Columns.Add("UnitCost",        typeof(string));
        table.Columns.Add("Amount",          typeof(string));

        foreach (var item in rr.Items.OrderBy(i => i.AcquisitionDate))
        {
            table.Rows.Add(
                item.Reference ?? string.Empty,
                item.PropertyNo,
                item.Description,
                item.AcquisitionDate.ToString("MM/dd/yyyy", nf),
                item.Quantity.ToString("N0", nf),
                item.UnitCost.ToString("N2", nf),
                item.Amount.ToString("N2", nf));
        }

        var padTo = Math.Max(minRows, table.Rows.Count);
        while (table.Rows.Count < padTo)
            table.Rows.Add(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        return table;
    }
}

internal sealed record SmrrFastHeader(
    string OrgName,
    string OrgAddress,
    string RrNo,
    string Date,
    string ReceivedFrom,
    string Address,
    string PurchaseCheck,
    string TransferCheck,
    string DonationCheck,
    string OtherCheck,
    string ReceivedByName,
    string ReceivedByDesignation,
    string NotedByName,
    string NotedByDesignation);
