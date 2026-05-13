using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.Finance.Domain.DisbursementVouchers;
using Shouldly;
using Xunit;

namespace Finance.Tests.Domain;

public sealed class DisbursementVoucherDomainTests
{
    [Fact]
    public void Create_ValidInput_CreatesDraftVoucher()
    {
        var dv = CreateDv();

        dv.Id.ShouldNotBe(Guid.Empty);
        dv.Status.ShouldBe(DisbursementVoucherStatus.Draft);
        dv.PaidDate.ShouldBeNull();
    }

    [Fact]
    public void Approve_WhenDraft_ChangesStatusToApproved()
    {
        var dv = CreateDv();

        dv.Approve();

        dv.Status.ShouldBe(DisbursementVoucherStatus.Approved);
    }

    [Fact]
    public void Approve_WhenAlreadyPaid_Throws()
    {
        var dv = CreateDv();
        dv.Approve();
        dv.Pay(DateOnly.FromDateTime(DateTime.UtcNow), null);

        var act = dv.Approve;

        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void Pay_WhenApproved_ChangesStatusToPaid()
    {
        var dv = CreateDv();
        dv.Approve();
        var paidDate = DateOnly.FromDateTime(DateTime.UtcNow);

        dv.Pay(paidDate, "Paid via check");

        dv.Status.ShouldBe(DisbursementVoucherStatus.Paid);
        dv.PaidDate.ShouldBe(paidDate);
    }

    [Fact]
    public void Pay_WhenNotApproved_Throws()
    {
        var dv = CreateDv();

        var act = () => dv.Pay(DateOnly.FromDateTime(DateTime.UtcNow), null);

        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void Return_WhenDraft_ChangesStatusToReturned()
    {
        var dv = CreateDv();

        dv.Return("Incomplete documents");

        dv.Status.ShouldBe(DisbursementVoucherStatus.Returned);
    }

    [Fact]
    public void Return_WhenPaid_Throws()
    {
        var dv = CreateDv();
        dv.Approve();
        dv.Pay(DateOnly.FromDateTime(DateTime.UtcNow), null);

        var act = () => dv.Return("Should not work");

        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_WhenPaid_Throws()
    {
        var dv = CreateDv();
        dv.Approve();
        dv.Pay(DateOnly.FromDateTime(DateTime.UtcNow), null);

        var act = () => dv.Cancel("Should not work");

        act.ShouldThrow<InvalidOperationException>();
    }

    private static DisbursementVoucher CreateDv() =>
        DisbursementVoucher.Create(
            dvNumber: "DV-2025-001",
            purchaseOrderId: Guid.NewGuid(),
            purchaseOrderNumber: "PO-2025-001",
            dvDate: DateOnly.FromDateTime(DateTime.UtcNow),
            fundCluster: "101",
            payee: "Acme Supplies Inc.",
            tinNo: "123-456-789",
            payeeAddress: "123 Main St",
            particulars: "Payment for office supplies",
            amount: 5000m,
            modeOfPayment: "Check",
            remarks: null);
}

