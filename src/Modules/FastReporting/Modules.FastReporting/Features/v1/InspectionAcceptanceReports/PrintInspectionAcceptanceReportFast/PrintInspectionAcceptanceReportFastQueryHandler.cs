using System.Data;
using System.Globalization;
using System.Reflection;
using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using AMIS.Modules.FastReporting.Services;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using FastReport;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.InspectionAcceptanceReports.PrintInspectionAcceptanceReportFast;

public sealed class PrintInspectionAcceptanceReportFastQueryHandler(IMediator mediator)
    : IQueryHandler<PrintInspectionAcceptanceReportFastQuery, ReportFileDto>
{
    private static readonly Assembly Assembly = typeof(PrintInspectionAcceptanceReportFastQueryHandler).Assembly;
    private const string TemplateName = "InspectionAcceptanceReportFast";

    public async ValueTask<ReportFileDto> Handle(PrintInspectionAcceptanceReportFastQuery query, CancellationToken cancellationToken)
    {
        var iar = await mediator.Send(new GetInspectionAcceptanceReportQuery(query.Id), cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Inspection and Acceptance Report '{query.Id}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        // Pull PO → PR to populate Requisitioning Office/Dept and Responsibility Center Code.
        // Both are nullable: if anything fails to resolve, fall back to empty strings — the
        // boxes still render, just blank for manual fill.
        var po = await mediator.Send(new GetPurchaseOrderQuery(iar.PurchaseOrderId), cancellationToken).ConfigureAwait(false);
        PurchaseRequestDto? pr = null;
        if (po is not null)
        {
            pr = await mediator.Send(new GetPurchaseRequestQuery(po.PurchaseRequestId), cancellationToken).ConfigureAwait(false);
        }

        var nf = CultureInfo.InvariantCulture;

        var poNumberAndDate = po is null
            ? iar.PoNumber
            : $"{iar.PoNumber} / {po.PoDate.ToString("MM/dd/yyyy", nf)}";

        var headerData = new List<IarFastHeader>
        {
            new(
                OrgName:                  org?.Name ?? string.Empty,
                OrgShortName:             string.IsNullOrWhiteSpace(org?.ShortName) ? string.Empty : $"({org!.ShortName})",
                OrgAddress:               org?.Address ?? string.Empty,
                IarNumber:                iar.IarNumber,
                IarDate:                  iar.IarDate.ToString("MM/dd/yyyy", nf),
                PoNumberAndDate:          poNumberAndDate,
                SupplierName:             iar.SupplierName,
                FundCluster:              po?.FundCluster ?? string.Empty,
                RequisitioningOffice:     pr?.DepartmentName ?? string.Empty,
                ResponsibilityCenterCode: pr?.ResponsibilityCenterCode ?? string.Empty,
                InvoiceNo:                iar.DeliveryReceiptNo ?? string.Empty,
                InvoiceDate:              iar.DeliveryDate?.ToString("MM/dd/yyyy", nf) ?? string.Empty,
                InspectionDate:           iar.InspectedOnUtc?.ToLocalTime().ToString("MM/dd/yyyy", nf) ?? string.Empty,
                AcceptanceDate:           iar.AcceptedOnUtc?.ToLocalTime().ToString("MM/dd/yyyy", nf) ?? string.Empty,
                InspectedCheck:           InspectedCheck(iar),
                CompleteCheck:            iar.Status == InspectionAcceptanceReportStatus.Accepted ? "X" : string.Empty,
                PartialCheck:             iar.Status == InspectionAcceptanceReportStatus.Inspected ? "X" : string.Empty,
                InspectorName:            (iar.InspectedByName ?? string.Empty).ToUpperInvariant(),
                // Acceptance signature = the user who actually accepted the IAR. Fall back to the assigned
                // custodian for records accepted before the acceptor was captured (or not yet accepted).
                CustodianName:            (string.IsNullOrWhiteSpace(iar.AcceptedByName) ? iar.ReceivedByName : iar.AcceptedByName ?? string.Empty).ToUpperInvariant())
        };

        var lineItemsTable = BuildLineItemsTable(iar, query.MinRows);

        return await FastReportService.GenerateAsync(
            Assembly,
            TemplateName,
            [
                new ReportDataSource("IarDS", headerData),
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
            fileName: $"IAR-{iar.IarNumber}",
            ct: cancellationToken).ConfigureAwait(false);
    }

    // DataTable (not List<record>) for line items — see note in PrintPurchaseRequestFastQueryHandler.
    private static DataTable BuildLineItemsTable(InspectionAcceptanceReportDto iar, int minRows)
    {
        var nf = CultureInfo.InvariantCulture;
        var table = new DataTable("LineItemsDS") { Locale = CultureInfo.InvariantCulture };
        table.Columns.Add("StockPropertyNo", typeof(string));
        table.Columns.Add("Description", typeof(string));
        table.Columns.Add("Unit", typeof(string));
        table.Columns.Add("Quantity", typeof(string));

        // Print only inspected lines (Passed/Rejected) — mirrors Exhibit 3 print page.
        var inspected = iar.LineItems
            .Where(li => li.InspectionResult != LineInspectionResult.Pending)
            .OrderBy(li => li.ItemNo)
            .ToList();

        foreach (var li in inspected)
        {
            table.Rows.Add(
                li.StockPropertyNo ?? string.Empty,
                li.Description,
                li.Unit,
                li.Quantity.ToString("N2", nf));
        }

        var padTo = Math.Max(minRows, table.Rows.Count);
        while (table.Rows.Count < padTo)
            table.Rows.Add(string.Empty, string.Empty, string.Empty, string.Empty);

        return table;
    }

    private static string InspectedCheck(InspectionAcceptanceReportDto iar)
    {
        var hasInspected = iar.LineItems.Any(li => li.InspectionResult != LineInspectionResult.Pending);
        return hasInspected ? "X" : string.Empty;
    }
}

internal sealed record IarFastHeader(
    string OrgName,
    string OrgShortName,
    string OrgAddress,
    string IarNumber,
    string IarDate,
    string PoNumberAndDate,
    string SupplierName,
    string FundCluster,
    string RequisitioningOffice,
    string ResponsibilityCenterCode,
    string InvoiceNo,
    string InvoiceDate,
    string InspectionDate,
    string AcceptanceDate,
    string InspectedCheck,
    string CompleteCheck,
    string PartialCheck,
    string InspectorName,
    string CustodianName);
