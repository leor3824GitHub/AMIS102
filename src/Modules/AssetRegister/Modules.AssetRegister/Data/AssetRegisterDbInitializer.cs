using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using AMIS.Modules.AssetRegister.Domain.Locations;
using AMIS.Modules.AssetRegister.Domain.Receiving;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.MasterData.Contracts.v1.Suppliers;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.AssetRegister.Data;

internal sealed class AssetRegisterDbInitializer(
    ILogger<AssetRegisterDbInitializer> logger,
    AssetRegisterDbContext context,
    IMediator mediator) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[{Tenant}] applied database migrations for asset register module", context.TenantInfo?.Identifier);
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedLocationsAsync(cancellationToken).ConfigureAwait(false);
        await SeedCatalogAsync(cancellationToken).ConfigureAwait(false);
        await SeedAcquisitionChainsAsync(cancellationToken).ConfigureAwait(false);
    }

    private static readonly (string Code, string Name, LocationType Type, string Description)[] SeedLocations =
    [
        ("WH-MAIN",   "Main Warehouse",          LocationType.Warehouse,  "Central warehouse for incoming and stored property."),
        ("SR-01",     "Supply Room 1",           LocationType.Storage,    "Primary supply room for semi-expendable items."),
        ("OFF-GSD",   "General Services Office",  LocationType.Office,     "General Services Department office."),
        ("OFF-ADMIN", "Administration Office",    LocationType.Office,     "Administrative offices and records."),
        ("DEPT-IT",   "IT Department",           LocationType.Department, "Information technology equipment and staff."),
        ("MOTORPOOL", "Motorpool",               LocationType.Other,      "Vehicle and heavy equipment staging area."),
    ];

    private async Task SeedLocationsAsync(CancellationToken cancellationToken)
    {
        if (bool.Parse(Environment.GetEnvironmentVariable("DISABLE_ASSETREGISTER_SEEDING") ?? "false"))
            return;

        var tenantId = context.TenantInfo?.Identifier ?? MultitenancyConstants.Root.Id;

        var existingCodes = (await context.Locations
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = SeedLocations
            .Where(l => !existingCodes.Contains(l.Code))
            .Select(l => Location.Create(tenantId, l.Code, l.Name, l.Type, parentLocationId: null, l.Description))
            .ToList();

        if (toAdd.Count == 0) return;

        await context.Locations.AddRangeAsync(toAdd, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("[{Tenant}] seeded asset register with {Count} new location(s)", tenantId, toAdd.Count);
    }

    private async Task SeedCatalogAsync(CancellationToken cancellationToken)
    {
        if (bool.Parse(Environment.GetEnvironmentVariable("DISABLE_ASSETREGISTER_SEEDING") ?? "false"))
        {
            logger.LogInformation("[{Tenant}] asset register seeding disabled by environment variable", context.TenantInfo?.Identifier);
            return;
        }

        var tenantId = context.TenantInfo?.Identifier ?? MultitenancyConstants.Root.Id;

        var existingCodes = (await context.PropertyItemCatalogs
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var seedItems = new[]
        {
            PropertyItemCatalog.Create(tenantId, "AR-SE-CHAIR", "Office Chair", "FF", "SE-FURN", "piece", "50203010", 5),
            PropertyItemCatalog.Create(tenantId, "AR-SE-TABLE", "Office Table", "FF", "SE-FURN", "piece", "50203010", 10),
            PropertyItemCatalog.Create(tenantId, "AR-SE-LAPTOP", "Laptop Computer", "DP", "SE-ICT", "unit", "50215030", 3),
            PropertyItemCatalog.Create(tenantId, "AR-PPE-DESKTOP", "Desktop Computer Set", "DP", "PPE-ICT", "set", "10605030", 5),
            PropertyItemCatalog.Create(tenantId, "AR-PPE-PRINTER", "Network Printer", "DP", "PPE-ICT", "unit", "10605030", 5),
            PropertyItemCatalog.Create(tenantId, "AR-PPE-AIRCON", "Split-type Air Conditioner", "EQUIP", "PPE-EQ", "unit", "10604010", 10),
            PropertyItemCatalog.Create(tenantId, "AR-PPE-TELEVISION", "Smart Television Set", "DP", "PPE-ICT", "unit", "10605030", 5),
            // Motor vehicles — DefaultPropertyClass "LT" (COA Account 10606010) so materialized assets
            // are auto-detected as vehicle-class PPE and become enrollable in the Vehicle module.
            PropertyItemCatalog.Create(tenantId, "AR-PPE-VEHICLE", "Motor Vehicle", AssetClassCodes.Vehicle, "PPE-LT", "unit", "10606010", 7),
        };

        var toAdd = seedItems.Where(x => !existingCodes.Contains(x.Code)).ToList();
        if (toAdd.Count == 0) return;

        await context.PropertyItemCatalogs.AddRangeAsync(toAdd, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("[{Tenant}] seeded asset register property item catalog with {Count} new entries", tenantId, toAdd.Count);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Demo receiving documents (dev seed)
    //
    // Receiving end of the two acquisition chains seeded on the Procurement side
    // (ProcurementDbInitializer.SeedAcquisitionChainsAsync): a PPERR for the PPE
    // chain and an SMRR for the semi-expendable chain. Each line materializes one
    // AssetRegistry row, mirroring CreateReceivingReportCommandHandler. The link
    // back to the IAR is the human-readable IAR number (item Reference) plus the
    // per-unit Property Number — there is no cross-module FK by design, so the
    // Property Numbers and IAR numbers below MUST match the Procurement seeder.
    // ──────────────────────────────────────────────────────────────────────────

    private sealed record SeedReceivingItem(
        string CatalogCode,
        string Description,
        decimal UnitCost,
        string? Brand,
        string? Model,
        string? SerialNo,
        string PropertyNo);

    private const string PpeReportNo = "PPERR-2026-06-9001";
    private const string SmrrReportNo = "SMRR-2026-06-9001";
    private const string VehicleReportNo = "PPERR-2026-06-9002";
    private const string PpeIarNumber = "IAR-2026-9001";
    private const string SeIarNumber = "IAR-2026-9002";
    private const string VehicleIarNumber = "IAR-2026-9003";

    private static readonly SeedReceivingItem[] PpeReceivingItems =
    [
        new("AR-PPE-DESKTOP", "Desktop Computer Set",       65_000m, "Dell",    "OptiPlex 7010",  "DSK-SN-0001", "2026-06-DP-0001"),
        new("AR-PPE-PRINTER", "Network Printer",            55_000m, "HP",      "LaserJet M428",  "PRN-SN-0001", "2026-06-DP-0002"),
        new("AR-PPE-AIRCON",  "Split-type Air Conditioner", 52_000m, "Carrier", "2.0HP Inverter", "AC-SN-0001",  "2026-06-OE-0001"),
    ];

    private static readonly SeedReceivingItem[] SeReceivingItems =
    [
        new("AR-SE-CHAIR",  "Office Chair",    4_500m,  "Uratex",       "Ergo Mesh",    null,          "2026-06-FF-0001"),
        new("AR-SE-TABLE",  "Office Table",    8_000m,  "Office Depot", "Exec Table",   null,          "2026-06-FF-0002"),
        new("AR-SE-LAPTOP", "Laptop Computer", 45_000m, "Lenovo",       "ThinkPad E14", "LAP-SN-0001", "2026-06-DP-0003"),
    ];

    // Motor-vehicle PPE lines — each materializes an LT-class PPE asset the Vehicle module enrolls.
    // PropertyNos here MUST match the spec map in VehicleDbInitializer so the seed can pair them.
    private static readonly SeedReceivingItem[] VehicleReceivingItems =
    [
        new("AR-PPE-VEHICLE", "Toyota Innova",            950_000m,   "Toyota",     "Innova",         "ENG-LT-0001", "2026-06-LT-0001"),
        new("AR-PPE-VEHICLE", "Toyota Vios",              850_000m,   "Toyota",     "Vios",           "ENG-LT-0002", "2026-06-LT-0002"),
        new("AR-PPE-VEHICLE", "Isuzu D-Max",              1_200_000m, "Isuzu",      "D-Max",          "ENG-LT-0003", "2026-06-LT-0003"),
        new("AR-PPE-VEHICLE", "Mitsubishi Montero Sport", 1_500_000m, "Mitsubishi", "Montero Sport",  "ENG-LT-0004", "2026-06-LT-0004"),
        new("AR-PPE-VEHICLE", "Hyundai H100",             1_100_000m, "Hyundai",    "H100",           "ENG-LT-0005", "2026-06-LT-0005"),
        new("AR-PPE-VEHICLE", "Honda City",               900_000m,   "Honda",      "City",           "ENG-LT-0006", "2026-06-LT-0006"),
    ];

    // Semi-expendable low-value threshold (COA Circular 2022-004): <= P5,000 is low-valued.
    private const decimal SeLowValueThreshold = 5_000m;

    private async Task SeedAcquisitionChainsAsync(CancellationToken cancellationToken)
    {
        if (bool.Parse(Environment.GetEnvironmentVariable("DISABLE_ASSETREGISTER_SEEDING") ?? "false"))
            return;

        var tenantId = context.TenantInfo?.Identifier ?? MultitenancyConstants.Root.Id;

        var reportNos = new[] { PpeReportNo, SmrrReportNo, VehicleReportNo };
        var alreadySeeded = await context.ReceivingReports
            .IgnoreQueryFilters()
            .AnyAsync(r => r.TenantId == tenantId && reportNos.Contains(r.ReportNo), cancellationToken)
            .ConfigureAwait(false);
        if (alreadySeeded) return;

        // Resolve real MasterData reference rows (custodian/noted-by + suppliers) so the
        // documents carry genuine employee ids and supplier names — matching the Procurement chains.
        var employees = (await mediator.Send(new SearchEmployeeReferencesQuery { PageSize = 1000, IsActive = true }, cancellationToken).ConfigureAwait(false))
            .Items.ToDictionary(e => e.EmployeeNumber, StringComparer.OrdinalIgnoreCase);
        var suppliers = (await mediator.Send(new ListSupplierReferencesQuery(), cancellationToken).ConfigureAwait(false))
            .ToDictionary(s => s.Code, StringComparer.OrdinalIgnoreCase);

        var custodian = employees.GetValueOrDefault("EMP-010"); // Roel Caperig — Property Custodian
        var notedByEmp = employees.GetValueOrDefault("EMP-001"); // Ricardo Santos — Head of Office
        var ppeSupplier = suppliers.GetValueOrDefault("SUP-007"); // TechSource Philippines
        var seSupplier = suppliers.GetValueOrDefault("SUP-010");  // Furnishings Co.

        if (custodian is null || notedByEmp is null || ppeSupplier is null || seSupplier is null)
        {
            logger.LogWarning(
                "[{Tenant}] skipping demo receiving reports: required MasterData reference rows (employees/suppliers) were not found.",
                tenantId);
            return;
        }

        var catalogByCode = (await context.PropertyItemCatalogs
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false))
            .ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        // EmployeeRef is an EF owned type — each ReceivingReport needs its OWN instances.
        // Reusing a single instance across both reports makes EF write NULL owned columns
        // for one of them (null value in column "ReceivedBy_EmployeeId").
        EmployeeRef ReceivedBy() => EmployeeRef.Create(custodian.Id, FullName(custodian), custodian.PositionName);
        EmployeeRef NotedBy() => EmployeeRef.Create(notedByEmp.Id, FullName(notedByEmp), notedByEmp.PositionName);

        var built = BuildReport(tenantId, ReceivingDocumentKind.PPERR, PpeReportNo, PpeIarNumber,
            ppeSupplier.Name, ppeSupplier.Address ?? string.Empty,
            PpeReceivingItems, date, ReceivedBy(), NotedBy(), catalogByCode);

        built &= BuildReport(tenantId, ReceivingDocumentKind.SMRR, SmrrReportNo, SeIarNumber,
            seSupplier.Name, seSupplier.Address ?? string.Empty,
            SeReceivingItems, date, ReceivedBy(), NotedBy(), catalogByCode);

        built &= BuildReport(tenantId, ReceivingDocumentKind.PPERR, VehicleReportNo, VehicleIarNumber,
            ppeSupplier.Name, ppeSupplier.Address ?? string.Empty,
            VehicleReceivingItems, date, ReceivedBy(), NotedBy(), catalogByCode);

        if (!built) return;

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "[{Tenant}] seeded demo receiving reports (PPERR + SMRR + vehicles) with {Count} materialized assets.",
            tenantId, PpeReceivingItems.Length + SeReceivingItems.Length + VehicleReceivingItems.Length);
    }

    private bool BuildReport(
        string tenantId,
        ReceivingDocumentKind kind,
        string reportNo,
        string iarNumber,
        string receivedFrom,
        string address,
        SeedReceivingItem[] items,
        DateOnly date,
        EmployeeRef receivedBy,
        EmployeeRef notedBy,
        IReadOnlyDictionary<string, PropertyItemCatalog> catalogByCode)
    {
        var report = ReceivingReport.Create(
            tenantId, kind, reportNo, date,
            receivedFrom, address, ReceiptType.Purchase, otherReceiptType: null,
            fundCluster: "01", receivedBy, notedBy, dateReceived: date);

        foreach (var item in items)
        {
            if (!catalogByCode.TryGetValue(item.CatalogCode, out var catalog))
            {
                logger.LogWarning(
                    "[{Tenant}] skipping demo receiving line '{Description}': catalog code '{Code}' not found.",
                    tenantId, item.Description, item.CatalogCode);
                return false;
            }

            var (assetType, category) = ClassifyFor(kind, item.UnitCost);

            report.AddItem(
                catalog.Id, reference: iarNumber, item.PropertyNo, item.Description,
                acquisitionDate: date, quantity: 1, item.UnitCost,
                item.SerialNo, item.Brand, item.Model, catalog.UacsObjectCode);

            var asset = AssetRegistry.Register(
                tenantId, catalog, assetType, category, PropertyNumber.Create(item.PropertyNo),
                item.Description, item.SerialNo, item.Brand, item.Model,
                fundCluster: "01", acquisitionDate: date, item.UnitCost,
                sourceIARId: null, sourcePurchaseOrderId: null);
            context.AssetRegistries.Add(asset);
        }

        context.ReceivingReports.Add(report);
        return true;
    }

    private static (AssetType assetType, AssetCategory category) ClassifyFor(ReceivingDocumentKind kind, decimal unitCost)
    {
        if (kind == ReceivingDocumentKind.PPERR)
            return (AssetType.PPE, AssetCategory.PPE);

        var seCategory = unitCost <= SeLowValueThreshold ? AssetCategory.LowValuedSemi : AssetCategory.HighValuedSemi;
        return (AssetType.SE, seCategory);
    }

    private static string FullName(EmployeeReferenceDto e) => $"{e.FirstName} {e.LastName}".Trim();
}

