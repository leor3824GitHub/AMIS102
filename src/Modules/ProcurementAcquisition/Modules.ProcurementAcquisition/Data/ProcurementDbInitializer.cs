using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.MasterData.Contracts.v1.Suppliers;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Domain.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Domain.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Domain.PurchaseRequests;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.ProcurementAcquisition.Data;

internal sealed class ProcurementDbInitializer(
    ILogger<ProcurementDbInitializer> logger,
    ProcurementDbContext context,
    IMediator mediator) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[{Tenant}] applied database migrations for procurement module", context.TenantInfo?.Identifier);
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedAcquisitionChainsAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("[{Tenant}] seeded procurement data.", context.TenantInfo?.Identifier);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Demo acquisition chains (dev seed)
    //
    // Two fully-worked PR → PO → IAR chains, one per asset type. The IARs are driven
    // through the real domain workflow (submit → certify → approve → issue →
    // inspect → assign property no → accept) so the documents are in their natural
    // terminal states. The PPERR / SMRR receiving documents and the materialized
    // AssetRegistry rows are seeded on the AssetRegister side
    // (AssetRegisterDbInitializer.SeedAcquisitionChainsAsync). The two sides connect
    // by matching business identifiers — IAR numbers and per-unit Property Numbers —
    // exactly as the real documents reference one another across the module boundary.
    //
    // All department / employee / supplier references are resolved to the REAL rows
    // seeded by MasterData (which seeds earlier in the same provisioning scope), so the
    // documents render correct names and link up in lookup-driven views.
    // Keep the SeedItem specs below in sync with the AssetRegister seeder.
    // ──────────────────────────────────────────────────────────────────────────

    private sealed record SeedItem(
        string Description,
        string Unit,
        decimal UnitCost,
        string Uacs,
        string? Brand,
        string? Model,
        string? SerialNo,
        string PropertyNo,
        string? PropertyClassHint);

    private sealed record ChainActors(
        EmployeeReferenceDto Requester,
        EmployeeReferenceDto Accountant,
        EmployeeReferenceDto Approver,
        EmployeeReferenceDto Inspector,
        EmployeeReferenceDto Custodian,
        string? ResponsibilityCenterCode);

    // PPE chain — every unit cost >= P50,000 capitalization threshold → received via PPERR.
    private const string PpePrNumber = "2026-06-9001";
    private const string PpePoNumber = "2026-06-901";
    private const string PpeIarNumber = "IAR-2026-9001";

    // SE chain — every unit cost < P50,000 → semi-expendable, received via SMRR.
    private const string SePrNumber = "2026-06-9002";
    private const string SePoNumber = "2026-06-902";
    private const string SeIarNumber = "IAR-2026-9002";

    private static readonly SeedItem[] PpeItems =
    [
        new("Desktop Computer Set", "set",  65_000m, "10605030", "Dell",    "OptiPlex 7010",   "DSK-SN-0001", "2026-06-DP-0001", "DP"),
        new("Network Printer",      "unit", 55_000m, "10605030", "HP",      "LaserJet M428",   "PRN-SN-0001", "2026-06-DP-0002", "DP"),
        new("Split-type Air Conditioner", "unit", 52_000m, "10604010", "Carrier", "2.0HP Inverter", "AC-SN-0001", "2026-06-OE-0001", "OE"),
    ];

    private static readonly SeedItem[] SeItems =
    [
        new("Office Chair", "piece", 4_500m,  "50203010", "Uratex",       "Ergo Mesh",   null,          "2026-06-FF-0001", "FF"),
        new("Office Table", "piece", 8_000m,  "50203010", "Office Depot", "Exec Table",  null,          "2026-06-FF-0002", "FF"),
        new("Laptop Computer", "unit", 45_000m, "50215030", "Lenovo",     "ThinkPad E14", "LAP-SN-0001", "2026-06-DP-0003", "DP"),
    ];

    private async Task SeedAcquisitionChainsAsync(CancellationToken cancellationToken)
    {
        if (bool.Parse(Environment.GetEnvironmentVariable("DISABLE_PROCUREMENT_SEEDING") ?? "false"))
        {
            logger.LogInformation("[{Tenant}] procurement seeding disabled by environment variable", context.TenantInfo?.Identifier);
            return;
        }

        var tenantId = context.TenantInfo?.Identifier ?? MultitenancyConstants.Root.Id;

        var seedPrNumbers = new[] { PpePrNumber, SePrNumber };
        var alreadySeeded = await context.PurchaseRequests
            .IgnoreQueryFilters()
            .AnyAsync(pr => pr.TenantId == tenantId && seedPrNumbers.Contains(pr.PrNumber), cancellationToken)
            .ConfigureAwait(false);
        if (alreadySeeded) return;

        // Resolve real MasterData reference rows (seeded earlier in this provisioning scope).
        var employees = (await mediator.Send(new SearchEmployeeReferencesQuery { PageSize = 1000, IsActive = true }, cancellationToken).ConfigureAwait(false))
            .Items.ToDictionary(e => e.EmployeeNumber, StringComparer.OrdinalIgnoreCase);
        var suppliers = (await mediator.Send(new ListSupplierReferencesQuery(), cancellationToken).ConfigureAwait(false))
            .ToDictionary(s => s.Code, StringComparer.OrdinalIgnoreCase);

        var requester = employees.GetValueOrDefault("EMP-015");   // Lena Uy — Supply
        var accountant = employees.GetValueOrDefault("EMP-002");  // Maria Reyes — Finance
        var approver = employees.GetValueOrDefault("EMP-001");    // Ricardo Santos — HoPE
        var inspector = employees.GetValueOrDefault("EMP-014");   // Anton Flores — Inspector
        var custodian = employees.GetValueOrDefault("EMP-010");   // Roel Caperig — Property Custodian
        var ppeSupplier = suppliers.GetValueOrDefault("SUP-007"); // TechSource Philippines
        var seSupplier = suppliers.GetValueOrDefault("SUP-010");  // Furnishings Co.

        if (requester is null || accountant is null || approver is null || inspector is null
            || custodian is null || ppeSupplier is null || seSupplier is null)
        {
            logger.LogWarning(
                "[{Tenant}] skipping procurement demo chains: required MasterData reference rows (employees/suppliers) were not found.",
                tenantId);
            return;
        }

        var department = await mediator.Send(new GetDepartmentReferenceByIdQuery(requester.DepartmentId), cancellationToken).ConfigureAwait(false);
        var actors = new ChainActors(requester, accountant, approver, inspector, custodian, department?.ResponsibilityCenterCode);

        BuildChain(tenantId, PpePrNumber, PpePoNumber, PpeIarNumber,
            "Procurement of ICT and office equipment for the Regional Office",
            ppeSupplier, PpeItems, actors);

        BuildChain(tenantId, SePrNumber, SePoNumber, SeIarNumber,
            "Procurement of semi-expendable office furniture and equipment",
            seSupplier, SeItems, actors);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("[{Tenant}] seeded 2 demo acquisition chains (PR → PO → IAR) with {Count} accepted items.",
            tenantId, PpeItems.Length + SeItems.Length);
    }

    private void BuildChain(
        string tenantId,
        string prNumber,
        string poNumber,
        string iarNumber,
        string purpose,
        SupplierReferenceDto supplier,
        SeedItem[] items,
        ChainActors actors)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // ── Purchase Request ───────────────────────────────────────────────────
        var pr = PurchaseRequest.Create(
            tenantId, prNumber, actors.Requester.DepartmentId, actors.ResponsibilityCenterCode,
            purpose, PrType.Unplanned, justification: "Demo seed data.",
            FullName(actors.Requester), saiNumber: null, saiDate: null, alobsNumber: null, alobsDate: null,
            items.Select(i => new PurchaseRequestLineItemData(
                Quantity: 1m, i.Unit, i.Description, i.UnitCost, CatalogItemId: null, i.Uacs, StockNumber: null)),
            actors.Requester.Id, actors.Requester.PositionName, ProcurementCategory.Asset);

        pr.Submit();
        var uacsByItemNo = items
            .Select((it, idx) => (ItemNo: idx + 1, it.Uacs))
            .ToDictionary(x => x.ItemNo, x => x.Uacs);
        pr.CertifyFundsAvailable(actors.Accountant.Id, FullName(actors.Accountant), uacsByItemNo,
            alobsNumber: null, alobsDate: null, actors.Accountant.PositionName);
        pr.Approve(FullName(actors.Approver), actors.Approver.Id, actors.Approver.PositionName);

        // ── Purchase Order ─────────────────────────────────────────────────────
        var po = PurchaseOrder.Create(
            tenantId, poNumber, pr.Id, canvassRequestId: null,
            supplier.Id, supplier.Name, supplier.Address ?? string.Empty, supplier.TinNo,
            ModeOfProcurement.SmallValueProcurement, placeOfDelivery: "Regional Office Supply Room",
            dateOfDelivery: today, deliveryTerm: "Within 30 days", paymentTerm: "Charge to Account",
            fundCluster: "01", oursBursNumber: null,
            items.Select(i => new PurchaseOrderLineItemData(
                StockNumber: null, i.Unit, i.Description, Quantity: 1m, i.UnitCost, CatalogItemId: null)),
            ProcurementCategory.Asset);

        po.Submit();
        po.CertifyFundsAvailable(actors.Accountant.Id, FullName(actors.Accountant),
            oursBursNumber: null, oursBursDate: null, fundCluster: "01", actors.Accountant.PositionName);
        po.Issue(actors.Approver.Id, FullName(actors.Approver), actors.Approver.PositionName);
        po.RecordDelivery(totalAcceptedQuantity: items.Length);

        // ── Inspection & Acceptance Report ──────────────────────────────────────
        var iar = InspectionAcceptanceReport.Create(
            tenantId, iarNumber, po.Id, supplier.Id, supplier.Name,
            actors.Inspector.Id, actors.Custodian.Id, deliveryReceiptNo: $"DR-{poNumber}", deliveryDate: today,
            remarks: "Complete and in good condition.",
            items.Select(i => new InspectionAcceptanceReportLineItemRequest(
                i.Description, TechnicalSpecifications: null, i.Brand, i.Model, i.SerialNo, i.PropertyClassHint,
                i.Unit, Quantity: 1m, i.UnitCost, InspectionRemarks: null,
                StockPropertyNo: null, CatalogItemId: null, i.Uacs, StockNumber: null)),
            ProcurementCategory.Asset);

        iar.SubmitForInspection();
        iar.RecordInspection(actors.Inspector.Id,
            items.Select((_, idx) => new LineInspectionDecision(idx + 1, LineInspectionResult.Passed, null)).ToList());
        for (var idx = 0; idx < items.Length; idx++)
            iar.AssignPropertyNo(idx + 1, items[idx].PropertyNo);
        iar.Accept(actors.Custodian.Id, FullName(actors.Custodian), actors.Custodian.PositionName);

        // Whole chain delivered & accepted — close the PR.
        pr.Complete();

        context.PurchaseRequests.Add(pr);
        context.PurchaseOrders.Add(po);
        context.InspectionAcceptanceReports.Add(iar);
    }

    private static string FullName(EmployeeReferenceDto e) => $"{e.FirstName} {e.LastName}".Trim();
}
