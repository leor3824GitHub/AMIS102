using System.Data;
using System.Globalization;
using System.Reflection;
using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using AMIS.Modules.FastReporting.Services;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using FastReport;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.PurchaseOrders.PrintPurchaseOrderFast;

public sealed class PrintPurchaseOrderFastQueryHandler(IMediator mediator)
    : IQueryHandler<PrintPurchaseOrderFastQuery, ReportFileDto>
{
    private static readonly Assembly Assembly = typeof(PrintPurchaseOrderFastQueryHandler).Assembly;
    private const string TemplateName = "PurchaseOrderFast";

    public async ValueTask<ReportFileDto> Handle(PrintPurchaseOrderFastQuery query, CancellationToken ct)
    {
        var po = await mediator.Send(new GetPurchaseOrderQuery(query.Id), ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Purchase order '{query.Id}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), ct).ConfigureAwait(false);

        var nf = CultureInfo.InvariantCulture;

        var headerData = new List<PoFastHeader>
        {
            new(
                OrgName:                  org?.Name ?? string.Empty,
                OrgShortName:             org?.ShortName ?? string.Empty,
                OrgAddress:               org?.Address ?? string.Empty,
                PoNumber:                 po.PoNumber,
                PoDate:                   po.PoDate.ToString("MM/dd/yyyy", nf),
                SupplierName:             po.SupplierName,
                SupplierAddress:          po.SupplierAddress,
                SupplierTin:              po.SupplierTin ?? string.Empty,
                ModeOfProcurement:        FormatMode(po.ModeOfProcurement),
                PlaceOfDelivery:          po.PlaceOfDelivery,
                DateOfDelivery:           po.DateOfDelivery?.ToString("MM/dd/yyyy", nf) ?? string.Empty,
                DeliveryTerm:             po.DeliveryTerm,
                PaymentTerm:              po.PaymentTerm,
                OursBursNumber:           po.OursBursNumber ?? string.Empty,
                TotalAmount:              po.TotalAmount.ToString("N2", nf),
                TotalAmountInWords:       po.TotalAmountInWords,
                AuthorizedOfficialName:        (org?.RegionalManagerName ?? string.Empty).ToUpperInvariant(),
                AuthorizedOfficialDesignation: org?.RegionalManagerDesignation ?? "Regional Manager II",
                AccountantName:                (org?.AccountantName ?? string.Empty).ToUpperInvariant())
        };

        var lineItemsTable = BuildLineItemsTable(po, query.MinRows);

        return await FastReportService.GenerateAsync(
            Assembly,
            TemplateName,
            [
                new ReportDataSource("PurchaseOrderDS", headerData),
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
            fileName: $"PO-{po.PoNumber}",
            ct: ct).ConfigureAwait(false);
    }

    // DataTable (not List<record>) for line items — see note in PrintPurchaseRequestFastQueryHandler.
    private static DataTable BuildLineItemsTable(PurchaseOrderDto po, int minRows)
    {
        var nf = CultureInfo.InvariantCulture;
        var table = new DataTable("LineItemsDS") { Locale = CultureInfo.InvariantCulture };
        table.Columns.Add("StockNumber", typeof(string));
        table.Columns.Add("Unit", typeof(string));
        table.Columns.Add("Description", typeof(string));
        table.Columns.Add("Quantity", typeof(string));
        table.Columns.Add("UnitCost", typeof(string));
        table.Columns.Add("Amount", typeof(string));

        foreach (var li in po.LineItems.OrderBy(x => x.ItemNo))
        {
            table.Rows.Add(
                li.StockNumber ?? string.Empty,
                li.Unit,
                li.Description,
                li.Quantity.ToString("N2", nf),
                li.UnitCost.ToString("N2", nf),
                li.Amount.ToString("N2", nf));
        }

        var padTo = Math.Max(minRows, table.Rows.Count);
        while (table.Rows.Count < padTo)
            table.Rows.Add(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        return table;
    }

    private static string FormatMode(ModeOfProcurement mode) => mode switch
    {
        ModeOfProcurement.DirectAcquisition     => "Direct Acquisition",
        ModeOfProcurement.SmallValueProcurement => "Small Value Procurement",
        ModeOfProcurement.PublicBidding         => "Public Bidding",
        ModeOfProcurement.NegotiatedProcurement => "Negotiated Procurement",
        ModeOfProcurement.ShoppingA             => "Shopping (A)",
        ModeOfProcurement.ShoppingB             => "Shopping (B)",
        _                                       => mode.ToString()
    };
}

internal sealed record PoFastHeader(
    string OrgName,
    string OrgShortName,
    string OrgAddress,
    string PoNumber,
    string PoDate,
    string SupplierName,
    string SupplierAddress,
    string SupplierTin,
    string ModeOfProcurement,
    string PlaceOfDelivery,
    string DateOfDelivery,
    string DeliveryTerm,
    string PaymentTerm,
    string OursBursNumber,
    string TotalAmount,
    string TotalAmountInWords,
    string AuthorizedOfficialName,
    string AuthorizedOfficialDesignation,
    string AccountantName);
