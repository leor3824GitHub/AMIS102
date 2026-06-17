using AMIS.Framework.Core.Context;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.Finance.Data;
using AMIS.Modules.Finance.Domain.DisbursementVouchers;
using AMIS.Modules.Finance.Features.v1.DisbursementVouchers.CreateDisbursementVoucher;
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

namespace Finance.Tests.Handlers;

/// <summary>
/// Guards the cross-module gate added so a Disbursement Voucher can only be raised against a Purchase
/// Order that has at least been Issued (COD/advance payments allowed pre-delivery), and never more than
/// once per PO. The PO lookup is an in-process Mediator query, mocked here.
/// </summary>
public sealed class CreateDisbursementVoucherCommandHandlerTests
{
    private const string Tenant = "test-tenant";

    [Theory]
    [InlineData(PurchaseOrderStatus.Draft)]
    [InlineData(PurchaseOrderStatus.PendingFundsAvailable)]
    [InlineData(PurchaseOrderStatus.PendingApproval)]
    [InlineData(PurchaseOrderStatus.Cancelled)]
    public async Task Handle_PoNotYetIssued_IsRejected(PurchaseOrderStatus status)
    {
        using var ctx = new FinanceTestContext(Tenant);
        var poId = Guid.NewGuid();
        var handler = CreateHandler(ctx.Db, PurchaseOrder(poId, status));

        var act = async () => await handler.Handle(Command(poId), CancellationToken.None);

        await act.ShouldThrowAsync<AMIS.Framework.Core.Exceptions.CustomException>();
        (await ctx.Db.DisbursementVouchers.CountAsync()).ShouldBe(0);
    }

    [Theory]
    [InlineData(PurchaseOrderStatus.Issued)]
    [InlineData(PurchaseOrderStatus.PartiallyDelivered)]
    [InlineData(PurchaseOrderStatus.Fulfilled)]
    public async Task Handle_PoIssuedOrLater_CreatesVoucher(PurchaseOrderStatus status)
    {
        using var ctx = new FinanceTestContext(Tenant);
        var poId = Guid.NewGuid();
        var handler = CreateHandler(ctx.Db, PurchaseOrder(poId, status));

        var id = await handler.Handle(Command(poId), CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);
        (await ctx.Db.DisbursementVouchers.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Handle_SecondVoucherForSamePo_IsRejected()
    {
        using var ctx = new FinanceTestContext(Tenant);
        var poId = Guid.NewGuid();
        var handler = CreateHandler(ctx.Db, PurchaseOrder(poId, PurchaseOrderStatus.Issued));

        await handler.Handle(Command(poId), CancellationToken.None);

        var act = async () => await handler.Handle(Command(poId), CancellationToken.None);
        await act.ShouldThrowAsync<AMIS.Framework.Core.Exceptions.CustomException>();
        (await ctx.Db.DisbursementVouchers.CountAsync()).ShouldBe(1);
    }

    private static CreateDisbursementVoucherCommandHandler CreateHandler(FinanceDbContext db, PurchaseOrderDto? po)
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

        return new CreateDisbursementVoucherCommandHandler(
            NullLogger<CreateDisbursementVoucherCommandHandler>.Instance, db, mediator, currentUser);
    }

    private static CreateDisbursementVoucherCommand Command(Guid poId) =>
        new(
            PurchaseOrderId: poId,
            PurchaseOrderNumber: "PO-2025-001",
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
/// Stands up a real <see cref="FinanceDbContext"/> over an isolated EF in-memory store with a fixed
/// tenant. Mirrors <c>ProcurementTestContext</c> in ProcurementAcquisition.Tests.
/// </summary>
internal sealed class FinanceTestContext : IDisposable
{
    public FinanceDbContext Db { get; }

    public FinanceTestContext(string tenantId)
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase($"finance-{Guid.NewGuid()}")
            .Options;

        var tenant = new AppTenantInfo(tenantId, tenantId, "Test Tenant");
        var accessor = new StaticTenantAccessor(new MultiTenantContext<AppTenantInfo>(tenant));
        var dbOptions = Options.Create(new DatabaseOptions { Provider = "InMemory" });

        Db = new FinanceDbContext(accessor, options, dbOptions, new TestHostEnvironment());
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
        public string ApplicationName { get; set; } = "Finance.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}
