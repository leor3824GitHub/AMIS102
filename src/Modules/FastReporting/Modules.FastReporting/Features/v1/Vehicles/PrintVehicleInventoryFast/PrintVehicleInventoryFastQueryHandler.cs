using System.Data;
using System.Globalization;
using System.Reflection;
using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using AMIS.Modules.FastReporting.Services;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using FastReport;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.Vehicles.PrintVehicleInventoryFast;

public sealed class PrintVehicleInventoryFastQueryHandler(IMediator mediator)
    : IQueryHandler<PrintVehicleInventoryFastQuery, ReportFileDto>
{
    private static readonly Assembly Assembly = typeof(PrintVehicleInventoryFastQueryHandler).Assembly;
    private const string TemplateName = "VehicleInventoryFast";
    private const string ReportType = "VehicleInventory";

    public async ValueTask<ReportFileDto> Handle(PrintVehicleInventoryFastQuery query, CancellationToken ct)
    {
        // MasterData queries share a DbContext — run sequentially, not concurrently.
        var inventory = await mediator.Send(
            new GetMotorVehicleInventoryQuery { Status = query.Status }, ct).ConfigureAwait(false);

        var org = await mediator.Send(new GetOrganizationProfileQuery(), ct).ConfigureAwait(false);

        var signatories = await mediator.Send(
            new GetReportSignatoriesQuery(ReportType), ct).ConfigureAwait(false);

        var nf = CultureInfo.InvariantCulture;
        var asOf = query.AsOfDate ?? DateTime.Today;

        var ordered = signatories
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ToList();

        var headerData = new List<VehicleInventoryFastHeader>
        {
            new(
                OrgName:    (org?.Name ?? string.Empty).ToUpperInvariant(),
                OrgAddress: org?.Address ?? string.Empty,
                AsOfDate:   $"as of {asOf.ToString("MMMM d, yyyy", nf)}",
                Sig1Label:  SignatoryAt(ordered, 0)?.Label ?? string.Empty,
                Sig1Name:   (SignatoryAt(ordered, 0)?.Name ?? string.Empty).ToUpperInvariant(),
                Sig1Title:  SignatoryAt(ordered, 0)?.Title ?? string.Empty,
                Sig2Label:  SignatoryAt(ordered, 1)?.Label ?? string.Empty,
                Sig2Name:   (SignatoryAt(ordered, 1)?.Name ?? string.Empty).ToUpperInvariant(),
                Sig2Title:  SignatoryAt(ordered, 1)?.Title ?? string.Empty,
                Sig3Label:  SignatoryAt(ordered, 2)?.Label ?? string.Empty,
                Sig3Name:   (SignatoryAt(ordered, 2)?.Name ?? string.Empty).ToUpperInvariant(),
                Sig3Title:  SignatoryAt(ordered, 2)?.Title ?? string.Empty)
        };

        var lineItemsTable = BuildLineItemsTable(inventory);

        return await FastReportService.GenerateAsync(
            Assembly,
            TemplateName,
            [
                new ReportDataSource("VehicleInventoryDS", headerData),
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
            fileName: $"InventoryOfMotorVehicles-{asOf:yyyyMMdd}",
            ct: ct).ConfigureAwait(false);
    }

    private static ReportSignatoryDto? SignatoryAt(IReadOnlyList<ReportSignatoryDto> list, int index) =>
        index < list.Count ? list[index] : null;

    private static DataTable BuildLineItemsTable(IReadOnlyList<MotorVehicleInventoryItemDto> items)
    {
        var nf = CultureInfo.InvariantCulture;
        var table = new DataTable("LineItemsDS") { Locale = CultureInfo.InvariantCulture };
        table.Columns.Add("Qty",               typeof(string));
        table.Columns.Add("Description",       typeof(string));
        table.Columns.Add("PlateNumber",       typeof(string));
        table.Columns.Add("VehicleUse",        typeof(string));
        table.Columns.Add("NoCyl",             typeof(string));
        table.Columns.Add("EngineCC",          typeof(string));
        table.Columns.Add("FuelType",          typeof(string));
        table.Columns.Add("Year",              typeof(string));
        table.Columns.Add("Cost",              typeof(string));
        table.Columns.Add("RunningCondition",  typeof(string));
        table.Columns.Add("AccountableOfficer", typeof(string));

        foreach (var v in items)
        {
            table.Rows.Add(
                v.Qty.ToString("N0", nf),
                BuildDescription(v),
                v.PlateNumber,
                v.VehicleUse ?? string.Empty,
                v.NumberOfCylinders?.ToString(nf) ?? string.Empty,
                v.EngineDisplacementCC?.ToString(nf) ?? string.Empty,
                v.FuelType ?? string.Empty,
                v.Year.ToString(nf),
                v.AcquisitionCost.HasValue ? v.AcquisitionCost.Value.ToString("N2", nf) : string.Empty,
                v.RunningCondition,
                BuildOfficer(v));
        }

        return table;
    }

    private static string BuildDescription(MotorVehicleInventoryItemDto v)
    {
        var parts = new List<string> { v.Description.ToUpperInvariant() };
        if (!string.IsNullOrWhiteSpace(v.MotorNumber)) parts.Add($"MOTOR NO. {v.MotorNumber}");
        if (!string.IsNullOrWhiteSpace(v.ChassisNumber)) parts.Add($"CHASSIS NO. {v.ChassisNumber}");
        if (!string.IsNullOrWhiteSpace(v.VehicleClassification)) parts.Add(v.VehicleClassification.ToUpperInvariant());
        return string.Join("\n", parts);
    }

    private static string BuildOfficer(MotorVehicleInventoryItemDto v)
    {
        if (string.IsNullOrWhiteSpace(v.AccountableOfficer)) return string.Empty;
        return string.IsNullOrWhiteSpace(v.AccountableOfficerTitle)
            ? v.AccountableOfficer
            : $"{v.AccountableOfficer}\n{v.AccountableOfficerTitle}";
    }
}

internal sealed record VehicleInventoryFastHeader(
    string OrgName,
    string OrgAddress,
    string AsOfDate,
    string Sig1Label,
    string Sig1Name,
    string Sig1Title,
    string Sig2Label,
    string Sig2Name,
    string Sig2Title,
    string Sig3Label,
    string Sig3Name,
    string Sig3Title);
