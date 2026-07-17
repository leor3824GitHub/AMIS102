using AMIS.Framework.Core.Context;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.BudgetDisbursement.Data;
using AMIS.Modules.BudgetDisbursement.Domain.BudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Domain.DisbursementVouchers;
using AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.CreateDisbursementVoucher;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace BudgetDisbursement.Tests.Handlers;

/// <summary>
/// Guards the BUR-first disburse flow: a Disbursement Voucher can only be raised against a Budget
/// Utilization Record that is Obligated, whose Purchase Order has at least been Issued (COD/advance
/// payments allowed pre-delivery), and never more than once per PO. Creating the DV utilizes the BUR.
/// The PO lookup is an in-process Mediator query, mocked here.
/// </summary>
public sealed class CreateDisbursementVoucherCommandHandlerTests
{
    private const string Tenant = "test-tenant";

    [Fact]
    public async Task Handle_BurNotObligated_IsRejected()
    {
        using var ctx = new BudgetDisbursementTestContext(Tenant);
        var poId = Guid.NewGuid();
        var bur = await SeedBur(ctx.Db, poId, obligate: false);
        var handler = CreateHandler(ctx.Db, PurchaseOrder(poId, PurchaseOrderStatus.Issued));

        var act = async () => await handler.Handle(Command(bur), CancellationToken.None);

        await act.ShouldThrowAsync<AMIS.Framework.Core.Exceptions.CustomException>();
        (await ctx.Db.DisbursementVouchers.CountAsync()).ShouldBe(0);
    }

    [Theory]
    [InlineData(PurchaseOrderStatus.Draft)]
    [InlineData(PurchaseOrderStatus.PendingFundsAvailable)]
    [InlineData(PurchaseOrderStatus.PendingApproval)]
    [InlineData(PurchaseOrderStatus.Cancelled)]
    public async Task Handle_PoNotYetIssued_IsRejected(PurchaseOrderStatus status)
    {
        using var ctx = new BudgetDisbursementTestContext(Tenant);
        var poId = Guid.NewGuid();
        var bur = await SeedBur(ctx.Db, poId, obligate: true);
        var handler = CreateHandler(ctx.Db, PurchaseOrder(poId, status));

        var act = async () => await handler.Handle(Command(bur), CancellationToken.None);

        await act.ShouldThrowAsync<AMIS.Framework.Core.Exceptions.CustomException>();
        (await ctx.Db.DisbursementVouchers.CountAsync()).ShouldBe(0);
    }

    [Theory]
    [InlineData(PurchaseOrderStatus.Issued)]
    [InlineData(PurchaseOrderStatus.PartiallyDelivered)]
    [InlineData(PurchaseOrderStatus.Fulfilled)]
    public async Task Handle_PoIssuedOrLater_CreatesVoucherAndUtilizesBur(PurchaseOrderStatus status)
    {
        using var ctx = new BudgetDisbursementTestContext(Tenant);
        var poId = Guid.NewGuid();
        var bur = await SeedBur(ctx.Db, poId, obligate: true);
        var handler = CreateHandler(ctx.Db, PurchaseOrder(poId, status));

        var id = await handler.Handle(Command(bur), CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);
        (await ctx.Db.DisbursementVouchers.CountAsync()).ShouldBe(1);

        // Creating the DV moves its BUR to Utilized and links the voucher back.
        var updated = await ctx.Db.BudgetUtilizationRequests.FindAsync(bur.Id);
        updated!.Status.ShouldBe(BudgetUtilizationRequestStatus.Utilized);
        updated.DisbursementVoucherId.ShouldBe(id);
    }

    [Fact]
    public async Task Handle_TwoVouchersDifferentPos_GetSequentialNumbers()
    {
        // Two DVs raised in sequence draw from the shared NumberSequences counter (SequenceKey "DV"),
        // so the second continues from the first — proving the unified allocator hands out sequential numbers.
        using var ctx = new BudgetDisbursementTestContext(Tenant);
        var year = DateOnly.FromDateTime(DateTime.UtcNow).Year;
        var po1 = Guid.NewGuid();
        var po2 = Guid.NewGuid();
        var bur1 = await SeedBur(ctx.Db, po1, obligate: true, burNumber: "BUR-2026-00001");
        var bur2 = await SeedBur(ctx.Db, po2, obligate: true, burNumber: "BUR-2026-00002");

        var id1 = await CreateHandler(ctx.Db, PurchaseOrder(po1, PurchaseOrderStatus.Issued))
            .Handle(Command(bur1), CancellationToken.None);
        var id2 = await CreateHandler(ctx.Db, PurchaseOrder(po2, PurchaseOrderStatus.Issued))
            .Handle(Command(bur2), CancellationToken.None);

        (await ctx.Db.DisbursementVouchers.FindAsync(id1))!.DvNumber.ShouldBe($"DV-{year}-00001");
        (await ctx.Db.DisbursementVouchers.FindAsync(id2))!.DvNumber.ShouldBe($"DV-{year}-00002");
    }

    [Fact]
    public async Task Handle_NoCounterRowButVouchersExist_SeedsFromHighestIssued()
    {
        // A voucher already issued this year but NO NumberSequences row (fresh year, or the counter table was
        // reset while vouchers remained). Allocation must seed from the highest issued number, never re-issue it.
        using var ctx = new BudgetDisbursementTestContext(Tenant);
        var year = DateOnly.FromDateTime(DateTime.UtcNow).Year;

        var priorDv = DisbursementVoucher.Create(
            Tenant, $"DV-{year}-00042", Guid.NewGuid(), "PO-prior", Guid.NewGuid(), "BUR-prior",
            DateOnly.FromDateTime(DateTime.UtcNow), "101", "Prior Payee", null, null, "Prior", 1000m, "Check", null);
        ctx.Db.DisbursementVouchers.Add(priorDv);
        await ctx.Db.SaveChangesAsync();

        var poId = Guid.NewGuid();
        var bur = await SeedBur(ctx.Db, poId, obligate: true, burNumber: "BUR-2026-00099");
        var id = await CreateHandler(ctx.Db, PurchaseOrder(poId, PurchaseOrderStatus.Issued))
            .Handle(Command(bur), CancellationToken.None);

        (await ctx.Db.DisbursementVouchers.FindAsync(id))!.DvNumber.ShouldBe($"DV-{year}-00043");
    }

    [Fact]
    public async Task Handle_SecondVoucherForSamePo_IsRejected()
    {
        using var ctx = new BudgetDisbursementTestContext(Tenant);
        var poId = Guid.NewGuid();
        // Two obligated BURs against the same PO; only one voucher may ever be raised for that PO.
        var bur1 = await SeedBur(ctx.Db, poId, obligate: true, burNumber: "BUR-2026-00001");
        var bur2 = await SeedBur(ctx.Db, poId, obligate: true, burNumber: "BUR-2026-00002");
        var handler = CreateHandler(ctx.Db, PurchaseOrder(poId, PurchaseOrderStatus.Issued));

        await handler.Handle(Command(bur1), CancellationToken.None);

        var act = async () => await handler.Handle(Command(bur2), CancellationToken.None);
        await act.ShouldThrowAsync<AMIS.Framework.Core.Exceptions.CustomException>();
        (await ctx.Db.DisbursementVouchers.CountAsync()).ShouldBe(1);
    }

    private static async Task<BudgetUtilizationRequest> SeedBur(
        BudgetDisbursementDbContext db, Guid poId, bool obligate, string burNumber = "BUR-2026-00001")
    {
        var bur = BudgetUtilizationRequest.Create(
            tenantId: "test-tenant",
            burNumber: burNumber,
            purchaseOrderId: poId,
            purchaseOrderNumber: "PO-2025-001",
            burDate: DateOnly.FromDateTime(DateTime.UtcNow),
            fundCluster: "01",
            allotmentClass: "MOOE",
            uacsObjectCode: "5-02-99-990",
            responsibilityCenter: null,
            particulars: "Office supplies",
            amount: 5000m,
            remarks: null);
        if (obligate) bur.Obligate();
        db.BudgetUtilizationRequests.Add(bur);
        await db.SaveChangesAsync();
        return bur;
    }

    private static CreateDisbursementVoucherCommandHandler CreateHandler(BudgetDisbursementDbContext db, PurchaseOrderDto? po)
    {
        var mediator = Substitute.For<IMediator>();
        // CA2012: configuring an NSubstitute return for a ValueTask-returning member is the intended
        // usage here, not a bug — the mock consumes the ValueTask exactly once.
#pragma warning disable CA2012
        mediator.Send(Arg.Any<GetPurchaseOrderQuery>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(po));
#pragma warning restore CA2012

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.GetUserId().Returns(Guid.NewGuid());
        // Stamped onto the new DV; must match the test context's ambient tenant or Finbuckle rejects the insert.
        currentUser.GetTenant().Returns(Tenant);

        return new CreateDisbursementVoucherCommandHandler(
            NullLogger<CreateDisbursementVoucherCommandHandler>.Instance, db, mediator, currentUser);
    }

    private static CreateDisbursementVoucherCommand Command(BudgetUtilizationRequest bur) =>
        new(
            BudgetUtilizationRequestId: bur.Id,
            BurNumber: bur.BurNumber,
            DvDate: DateOnly.FromDateTime(DateTime.UtcNow),
            FundCluster: "101",
            Payee: "Acme Supplies Inc.",
            TinNo: null,
            PayeeAddress: null,
            Particulars: "Payment for PO-2025-001",
            Amount: 5000m,
            ModeOfPayment: "Check",
            Remarks: null);

    private static PurchaseOrderDto PurchaseOrder(Guid id, PurchaseOrderStatus status) =>
        new(
            Id: id,
            PoNumber: "PO-2025-001",
            PoDate: DateOnly.FromDateTime(DateTime.UtcNow),
            PurchaseRequestId: Guid.NewGuid(),
            PrNumber: "PR-2025-001",
            CanvassRequestId: null,
            RivNumber: null,
            SupplierId: Guid.NewGuid(),
            SupplierName: "Acme Supplies Inc.",
            SupplierAddress: "123 Main St",
            SupplierTin: "123-456-789",
            ModeOfProcurement: ModeOfProcurement.SmallValueProcurement,
            PlaceOfDelivery: "Warehouse",
            DateOfDelivery: null,
            DeliveryTerm: "30 days",
            PaymentTerm: "COD",
            FundCluster: "101",
            OursBursNumber: null,
            OursBursDate: null,
            Status: status,
            LineItems: [],
            TotalAmount: 5000m,
            TotalAmountInWords: "Five thousand pesos",
            CreatedOnUtc: DateTimeOffset.UtcNow,
            CreatedBy: null,
            LastModifiedOnUtc: null);
}

/// <summary>
/// Stands up a real <see cref="BudgetDisbursementDbContext"/> over an isolated EF in-memory store with a fixed
/// tenant. Mirrors <c>ProcurementTestContext</c> in ProcurementAcquisition.Tests.
/// </summary>
internal sealed class BudgetDisbursementTestContext : IDisposable
{
    public BudgetDisbursementDbContext Db { get; }

    public BudgetDisbursementTestContext(string tenantId)
    {
        var options = new DbContextOptionsBuilder<BudgetDisbursementDbContext>()
            .UseInMemoryDatabase($"finance-{Guid.NewGuid()}")
            .Options;

        var tenant = new AppTenantInfo(tenantId, tenantId, "Test Tenant");
        var accessor = new StaticTenantAccessor(new MultiTenantContext<AppTenantInfo>(tenant));
        var dbOptions = Options.Create(new DatabaseOptions { Provider = "InMemory" });

        Db = new BudgetDisbursementDbContext(accessor, options, dbOptions, new TestHostEnvironment());
    }

    public void Dispose() => Db.Dispose();

    private sealed class StaticTenantAccessor(IMultiTenantContext<AppTenantInfo> context)
        : IMultiTenantContextAccessor<AppTenantInfo>
    {
        public IMultiTenantContext MultiTenantContext { get; } = context;

        IMultiTenantContext<AppTenantInfo> IMultiTenantContextAccessor<AppTenantInfo>.MultiTenantContext =>
            (IMultiTenantContext<AppTenantInfo>)MultiTenantContext;
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "BudgetDisbursement.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}
