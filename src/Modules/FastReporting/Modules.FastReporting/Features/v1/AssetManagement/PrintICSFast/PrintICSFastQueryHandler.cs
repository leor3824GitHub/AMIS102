using System.Data;
using System.Globalization;
using System.Reflection;
using AMIS.Modules.AssetManagement.Contracts.v1.InventoryCustodianSlips;
using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using AMIS.Modules.FastReporting.Services;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.References;
using FastReport;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.AssetManagement.PrintICSFast;

public sealed class PrintICSFastQueryHandler(IMediator mediator)
    : IQueryHandler<PrintICSFastQuery, ReportFileDto>
{
    private static readonly Assembly Assembly = typeof(PrintICSFastQueryHandler).Assembly;
    private const string TemplateName = "InventoryCustodianSlipFast";

    public async ValueTask<ReportFileDto> Handle(PrintICSFastQuery query, CancellationToken ct)
    {
        var ics = await mediator.Send(new GetICSForPrintQuery(query.Id), ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Inventory Custodian Slip '{query.Id}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), ct).ConfigureAwait(false);

        // Batch-fetch both employee references in one query.
        var employeeIds = new HashSet<Guid> { ics.ReceivedByEmployeeId };
        if (ics.IssuedFromEmployeeId.HasValue)
            employeeIds.Add(ics.IssuedFromEmployeeId.Value);

        var employees = await mediator.Send(
            new GetEmployeeReferencesByIdsQuery(employeeIds), ct).ConfigureAwait(false);

        employees.TryGetValue(ics.ReceivedByEmployeeId, out var receivedBy);
        EmployeeReferenceDto? issuedFrom = null;
        if (ics.IssuedFromEmployeeId.HasValue)
            employees.TryGetValue(ics.IssuedFromEmployeeId.Value, out issuedFrom);

        var nf = CultureInfo.InvariantCulture;

        var headerData = new List<IcsFastHeader>
        {
            new(
                EntityName:          org?.Name ?? string.Empty,
                FundCluster:         ics.FundCluster ?? string.Empty,
                ICSNo:               ics.ICSNo,
                ICSDate:             ics.Date.ToString("MM/dd/yyyy", nf),
                IssuedFromName:      FullName(issuedFrom),
                IssuedFromPosition:  issuedFrom?.PositionName ?? string.Empty,
                IssuedFromOffice:    issuedFrom?.OfficeName ?? string.Empty,
                ReceivedByName:      FullName(receivedBy),
                ReceivedByPosition:  receivedBy?.PositionName ?? string.Empty,
                ReceivedByOffice:    receivedBy?.OfficeName ?? string.Empty)
        };

        var lineItemsTable = BuildLineItemsTable(ics, query.MinRows);

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
            fileName: $"ICS-{ics.ICSNo}",
            ct: ct).ConfigureAwait(false);
    }

    private static DataTable BuildLineItemsTable(ICSForPrintDto ics, int minRows)
    {
        var nf = CultureInfo.InvariantCulture;
        var table = new DataTable("LineItemsDS") { Locale = CultureInfo.InvariantCulture };
        table.Columns.Add("Quantity",          typeof(string));
        table.Columns.Add("Unit",              typeof(string));
        table.Columns.Add("UnitCost",          typeof(string));
        table.Columns.Add("TotalCost",         typeof(string));
        table.Columns.Add("Description",       typeof(string));
        table.Columns.Add("InventoryItemNo",   typeof(string));
        table.Columns.Add("EstUsefulLife",     typeof(string));

        foreach (var item in ics.Items.OrderBy(i => i.ItemNo))
        {
            var description = string.IsNullOrWhiteSpace(item.Description)
                ? item.ItemName
                : item.Description;
            var eul = item.EstimatedUsefulLifeYears.HasValue
                ? $"{item.EstimatedUsefulLifeYears} yr(s)"
                : string.Empty;

            table.Rows.Add(
                "1",
                item.UnitOfMeasure,
                item.UnitCost.ToString("N2", nf),
                item.UnitCost.ToString("N2", nf),  // qty = 1, so total = unit cost
                description,
                item.PropertyNo,
                eul);
        }

        var padTo = Math.Max(minRows, table.Rows.Count);
        while (table.Rows.Count < padTo)
            table.Rows.Add(
                string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty);

        return table;
    }

    private static string FullName(EmployeeReferenceDto? emp) =>
        emp is null
            ? string.Empty
            : $"{emp.FirstName} {emp.LastName}".Trim().ToUpperInvariant();
}

internal sealed record IcsFastHeader(
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
