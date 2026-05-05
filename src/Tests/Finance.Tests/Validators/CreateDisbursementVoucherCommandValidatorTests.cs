using FSH.Modules.Finance.Contracts.v1.DisbursementVouchers;
using FSH.Modules.Finance.Features.v1.DisbursementVouchers.CreateDisbursementVoucher;
using Shouldly;
using Xunit;

namespace Finance.Tests.Validators;

public sealed class CreateDisbursementVoucherCommandValidatorTests
{
    private readonly CreateDisbursementVoucherCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var command = ValidCommand();

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_EmptyPurchaseOrderId_Fails()
    {
        var command = ValidCommand() with { PurchaseOrderId = Guid.Empty };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.PurchaseOrderId));
    }

    [Fact]
    public void Validate_EmptyPayee_Fails()
    {
        var command = ValidCommand() with { Payee = string.Empty };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Payee));
    }

    [Fact]
    public void Validate_ZeroAmount_Fails()
    {
        var command = ValidCommand() with { Amount = 0m };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Amount));
    }

    [Fact]
    public void Validate_NegativeAmount_Fails()
    {
        var command = ValidCommand() with { Amount = -100m };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Amount));
    }

    [Fact]
    public void Validate_EmptyParticulars_Fails()
    {
        var command = ValidCommand() with { Particulars = string.Empty };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Particulars));
    }

    [Fact]
    public void Validate_EmptyFundCluster_Fails()
    {
        var command = ValidCommand() with { FundCluster = string.Empty };

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.FundCluster));
    }

    private static CreateDisbursementVoucherCommand ValidCommand() =>
        new(
            PurchaseOrderId: Guid.NewGuid(),
            PurchaseOrderNumber: "PO-2025-001",
            DvDate: DateOnly.FromDateTime(DateTime.UtcNow),
            FundCluster: "101",
            Payee: "Acme Supplies Inc.",
            TinNo: "123-456-789",
            PayeeAddress: "123 Main St",
            Particulars: "Payment for office supplies per PO-2025-001",
            Amount: 5000m,
            ModeOfPayment: "Check",
            Remarks: null);
}
