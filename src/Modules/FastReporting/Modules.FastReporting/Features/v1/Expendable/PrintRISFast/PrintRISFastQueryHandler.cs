using System.Data;
using System.Globalization;
using System.Reflection;
using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using AMIS.Modules.FastReporting.Services;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.References;
using FastReport;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.Expendable.PrintRISFast;

public sealed class PrintRISFastQueryHandler(IMediator mediator)
    : IQueryHandler<PrintRISFastQuery, ReportFileDto>
{
    private static readonly Assembly Assembly = typeof(PrintRISFastQueryHandler).Assembly;
    private const string TemplateName = "RequisitionAndIssueSlipFast";
    private const string Check = "✓"; // ✓

    public async ValueTask<ReportFileDto> Handle(PrintRISFastQuery query, CancellationToken ct)
    {
        var ris = await mediator.Send(new GetSupplyRequestQuery(query.Id), ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Supply request '{query.Id}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), ct).ConfigureAwait(false);

        // Signatories — EmployeeId / ApprovedBy are identity user id strings (see SupplyRequest handlers).
        var requester = await ResolveEmployeeAsync(ris.EmployeeId, ct).ConfigureAwait(false);
        var issuer = await ResolveEmployeeAsync(ris.ApprovedBy, ct).ConfigureAwait(false);

        // Office ← requester's office; Section ← request department (resolved by id when possible).
        var section = await ResolveDepartmentNameAsync(ris.DepartmentId, ct).ConfigureAwait(false);
        var office = requester?.OfficeName ?? string.Empty;

        var nf = CultureInfo.InvariantCulture;

        var headerData = new List<RisFastHeader>
        {
            new(
                EntityName:               org?.Name ?? string.Empty,
                FundCluster:              string.Empty,
                Section:                  section,
                Office:                   office,
                ResponsibilityCenterCode: string.Empty,
                RisNo:                    ris.RequestNumber,
                RisDate:                  ris.RequestDate.ToString("MM/dd/yyyy", nf),
                Purpose:                  ris.BusinessJustification ?? string.Empty,
                RequestedByName:          FullName(requester),
                RequestedByDesignation:   requester?.PositionName ?? string.Empty,
                ApprovedByName:           (org?.ApprovingOfficialName ?? string.Empty).ToUpperInvariant(),
                ApprovedByDesignation:    org?.ApprovingOfficialDesignation ?? string.Empty,
                IssuedByName:             FullName(issuer),
                IssuedByDesignation:      issuer?.PositionName ?? string.Empty,
                ReceivedByName:           FullName(requester),
                ReceivedByDesignation:    requester?.PositionName ?? string.Empty)
        };

        var lineItemsTable = await BuildLineItemsTableAsync(ris, query.MinRows, ct).ConfigureAwait(false);

        return await FastReportService.GenerateAsync(
            Assembly,
            TemplateName,
            [
                new ReportDataSource("RisDS", headerData),
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
            fileName: $"RIS-{ris.RequestNumber}",
            ct: ct).ConfigureAwait(false);
    }

    private async Task<DataTable> BuildLineItemsTableAsync(SupplyRequestDto ris, int minRows, CancellationToken ct)
    {
        var table = new DataTable("LineItemsDS") { Locale = CultureInfo.InvariantCulture };
        table.Columns.Add("StockNo",        typeof(string));
        table.Columns.Add("Unit",           typeof(string));
        table.Columns.Add("Description",    typeof(string));
        table.Columns.Add("RequisitionQty", typeof(string));
        table.Columns.Add("AvailableYes",   typeof(string));
        table.Columns.Add("AvailableNo",    typeof(string));
        table.Columns.Add("IssueQty",       typeof(string));
        table.Columns.Add("Remarks",        typeof(string));

        foreach (var item in ris.Items)
        {
            var product = await mediator.Send(new GetProductQuery(item.ProductId), ct).ConfigureAwait(false);

            var issuedQty = item.FulfilledQuantity > 0 ? item.FulfilledQuantity : item.ApprovedQuantity;
            var available = item.RequestedQuantity > 0 && issuedQty >= item.RequestedQuantity;
            var remarks = item.FulfilledQuantity > 0 ? "Served" : (item.Notes ?? string.Empty);

            table.Rows.Add(
                product?.StockNo ?? string.Empty,
                product?.UnitOfMeasure ?? string.Empty,
                product?.Name ?? string.Empty,
                item.RequestedQuantity.ToString(CultureInfo.InvariantCulture),
                available ? Check : string.Empty,
                item.RequestedQuantity > 0 && !available ? Check : string.Empty,
                issuedQty > 0 ? issuedQty.ToString(CultureInfo.InvariantCulture) : string.Empty,
                remarks);
        }

        var padTo = Math.Max(minRows, table.Rows.Count);
        while (table.Rows.Count < padTo)
            table.Rows.Add(
                string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty);

        return table;
    }

    private async Task<EmployeeReferenceDto?> ResolveEmployeeAsync(string? identityUserId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(identityUserId))
            return null;

        return await mediator.Send(
            new GetEmployeeReferenceByIdentityUserIdQuery(identityUserId), ct).ConfigureAwait(false);
    }

    private async Task<string> ResolveDepartmentNameAsync(string? departmentId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(departmentId))
            return string.Empty;

        if (!Guid.TryParse(departmentId, out var deptGuid))
            return departmentId;

        var dept = await mediator.Send(new GetDepartmentReferenceByIdQuery(deptGuid), ct).ConfigureAwait(false);
        return dept?.Name ?? departmentId;
    }

    private static string FullName(EmployeeReferenceDto? emp) =>
        emp is null
            ? string.Empty
            : $"{emp.FirstName} {emp.LastName}".Trim().ToUpperInvariant();
}

internal sealed record RisFastHeader(
    string EntityName,
    string FundCluster,
    string Section,
    string Office,
    string ResponsibilityCenterCode,
    string RisNo,
    string RisDate,
    string Purpose,
    string RequestedByName,
    string RequestedByDesignation,
    string ApprovedByName,
    string ApprovedByDesignation,
    string IssuedByName,
    string IssuedByDesignation,
    string ReceivedByName,
    string ReceivedByDesignation);
