using AMIS.Framework.Core.Context;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Domain.Canvass;
using AMIS.Modules.ProcurementAcquisition.Domain.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CreatePurchaseOrdersFromCanvass;
using NSubstitute;
using Shouldly;
using Xunit;

namespace ProcurementAcquisition.Tests.Integration;

/// <summary>
/// Verifies the split-award fan-out: a canvass awarded line-by-line to different suppliers generates exactly
/// one purchase order per winning supplier, each carrying only that supplier's won lines at the awarded prices.
/// </summary>
public sealed class CreatePurchaseOrdersFromCanvassTests
{
    private const string Tenant = "test-tenant";

    [Fact]
    public async Task Handle_TwoSuppliersWinningDifferentLines_GeneratesOnePoPerSupplier()
    {
        using var ctx = new ProcurementTestContext(Tenant);

        var supplierA = Guid.NewGuid();
        var supplierB = Guid.NewGuid();
        var (canvassId, _) = await SeedAwardedCanvassAsync(ctx, supplierA, supplierB);

        var handler = new CreatePurchaseOrdersFromCanvassCommandHandler(ctx.Db, CurrentUser());
        var result = await handler.Handle(
            new CreatePurchaseOrdersFromCanvassCommand(
                canvassId, ModeOfProcurement.SmallValueProcurement, "Warehouse", null, "30 days", "On delivery", null, null),
            CancellationToken.None);

        result.Count.ShouldBe(2);

        var poA = result.Single(p => p.SupplierId == supplierA);
        var poB = result.Single(p => p.SupplierId == supplierB);

        // Supplier A won line 1 @ 120; Supplier B won line 2 @ 80.
        poA.LineItems.ShouldHaveSingleItem().UnitCost.ShouldBe(120m);
        poB.LineItems.ShouldHaveSingleItem().UnitCost.ShouldBe(80m);
        poA.CanvassRequestId.ShouldBe(canvassId);
        poB.CanvassRequestId.ShouldBe(canvassId);
    }

    [Fact]
    public async Task Handle_RerunAfterAllOrdered_IsIdempotent()
    {
        using var ctx = new ProcurementTestContext(Tenant);
        var (canvassId, _) = await SeedAwardedCanvassAsync(ctx, Guid.NewGuid(), Guid.NewGuid());

        var handler = new CreatePurchaseOrdersFromCanvassCommandHandler(ctx.Db, CurrentUser());
        var command = new CreatePurchaseOrdersFromCanvassCommand(
            canvassId, ModeOfProcurement.SmallValueProcurement, "Warehouse", null, "30 days", "On delivery", null, null);

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        first.Count.ShouldBe(2);
        second.Count.ShouldBe(0); // every supplier already has a PO — nothing to generate
    }

    private static ICurrentUser CurrentUser()
    {
        var user = Substitute.For<ICurrentUser>();
        user.GetTenant().Returns(Tenant);
        user.GetUserId().Returns(Guid.NewGuid());
        return user;
    }

    private static async Task<(Guid CanvassId, Guid PrId)> SeedAwardedCanvassAsync(
        ProcurementTestContext ctx, Guid supplierA, Guid supplierB)
    {
        // Source PR — Supply category, two screened lines (StockNumber present so the resolver passes).
        var prLines = new[]
        {
            new PurchaseRequestLineItemData(Quantity: 10m, UnitOfIssue: "piece", ItemDescription: "Bond Paper",
                EstimatedUnitCost: 130m, StockNumber: "STK-1"),
            new PurchaseRequestLineItemData(Quantity: 20m, UnitOfIssue: "piece", ItemDescription: "Ballpen",
                EstimatedUnitCost: 90m, StockNumber: "STK-2"),
        };
        var pr = PurchaseRequest.Create(
            tenantId: Tenant,
            prNumber: "PR-2026-0001",
            departmentId: Guid.NewGuid(),
            responsibilityCenterCode: null,
            purpose: "Office supplies",
            prType: PrType.Planned,
            justification: null,
            requestedByName: "Requester",
            saiNumber: null,
            saiDate: null,
            alobsNumber: null,
            alobsDate: null,
            lineItems: prLines,
            requestedById: Guid.NewGuid(),
            category: ProcurementCategory.Supply);
        ctx.Db.PurchaseRequests.Add(pr);

        // Canvass covering both PR lines.
        var canvass = CanvassRequest.Create(Tenant, "RIV-2026-0001", pr.Id,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            new[]
            {
                new CanvassRequestLineItemData(1, "Bond Paper", "piece", 10m, 130m),
                new CanvassRequestLineItemData(2, "Ballpen", "piece", 20m, 90m),
            });

        // Two supplier quotations, each quoting both lines (PrItemNo stamped).
        var quoteA = CanvassQuotation.Create(Tenant, canvass.Id, supplierA, "Supplier A", "Addr A", null,
            DateOnly.FromDateTime(DateTime.UtcNow), null,
            new[] { (1, "Bond Paper", "piece", 10m, 120m), (2, "Ballpen", "piece", 20m, 95m) });
        var quoteB = CanvassQuotation.Create(Tenant, canvass.Id, supplierB, "Supplier B", "Addr B", null,
            DateOnly.FromDateTime(DateTime.UtcNow), null,
            new[] { (1, "Bond Paper", "piece", 10m, 140m), (2, "Ballpen", "piece", 20m, 80m) });
        canvass.Quotations.Add(quoteA);
        canvass.Quotations.Add(quoteB);

        // Award line 1 → A @ 120, line 2 → B @ 80.
        canvass.AwardLines(new Dictionary<int, (Guid, Guid, decimal)>
        {
            [1] = (quoteA.Id, supplierA, 120m),
            [2] = (quoteB.Id, supplierB, 80m),
        });

        ctx.Db.CanvassRequests.Add(canvass);
        ctx.Db.CanvassQuotations.Add(quoteA);
        ctx.Db.CanvassQuotations.Add(quoteB);
        await ctx.Db.SaveChangesAsync();

        return (canvass.Id, pr.Id);
    }
}
