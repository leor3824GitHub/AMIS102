using FSH.Modules.AssetManagement.Domain;
using FSH.Modules.AssetManagement.Features.v1.TangibleInventory.CreateTangibleInventory;
using Shouldly;
using Xunit;

namespace AssetManagement.Tests.Validators;

public sealed class CreateTangibleInventoryCommandValidatorTests
{
    private readonly CreateTangibleInventoryCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _sut.Validate(ValidCommand());
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_EmptyReportNo_Fails()
    {
        var command = ValidCommand() with { ReportNo = string.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.ReportNo));
    }

    [Fact]
    public void Validate_ReportNoExceeds32Characters_Fails()
    {
        var command = ValidCommand() with { ReportNo = new string('R', 33) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.ReportNo));
    }

    [Fact]
    public void Validate_DefaultDate_Fails()
    {
        var command = ValidCommand() with { Date = default };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Date));
    }

    [Fact]
    public void Validate_EmptyReceivedFrom_Fails()
    {
        var command = ValidCommand() with { ReceivedFrom = string.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.ReceivedFrom));
    }

    [Fact]
    public void Validate_ReceivedFromExceeds200Characters_Fails()
    {
        var command = ValidCommand() with { ReceivedFrom = new string('V', 201) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.ReceivedFrom));
    }

    [Fact]
    public void Validate_AddressExceeds500Characters_Fails()
    {
        var command = ValidCommand() with { Address = new string('A', 501) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Address));
    }

    [Fact]
    public void Validate_NullAddress_Passes()
    {
        var command = ValidCommand() with { Address = null };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReceiptTypeOthersWithOtherReceiptType_Passes()
    {
        var command = ValidCommand() with
        {
            ReceiptType = ReceiptType.Others,
            OtherReceiptType = "Custom Receipt"
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReceiptTypeOthersWithoutOtherReceiptType_Fails()
    {
        var command = ValidCommand() with
        {
            ReceiptType = ReceiptType.Others,
            OtherReceiptType = null
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.OtherReceiptType));
    }

    [Fact]
    public void Validate_ReceiptTypeOthersWithEmptyOtherReceiptType_Fails()
    {
        var command = ValidCommand() with
        {
            ReceiptType = ReceiptType.Others,
            OtherReceiptType = string.Empty
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.OtherReceiptType));
    }

    [Fact]
    public void Validate_ReceiptTypeOthersOtherReceiptTypeExceeds100Characters_Fails()
    {
        var command = ValidCommand() with
        {
            ReceiptType = ReceiptType.Others,
            OtherReceiptType = new string('O', 101)
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.OtherReceiptType));
    }

    [Theory]
    [InlineData(ReceiptType.Purchase)]
    [InlineData(ReceiptType.Transfer)]
    [InlineData(ReceiptType.Donation)]
    public void Validate_NonOthersReceiptTypeWithoutOtherReceiptType_Passes(ReceiptType receiptType)
    {
        var command = ValidCommand() with
        {
            ReceiptType = receiptType,
            OtherReceiptType = null
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_FundClusterExceeds50Characters_Fails()
    {
        var command = ValidCommand() with { FundCluster = new string('F', 51) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.FundCluster));
    }

    [Fact]
    public void Validate_EmptyItems_Fails()
    {
        var command = ValidCommand() with { Items = [] };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Items));
    }

    [Fact]
    public void Validate_ItemWithEmptyTangibleItemId_Fails()
    {
        var command = ValidCommand() with
        {
            Items = [new CreateTangibleInventoryItemRequest(Guid.Empty, null)]
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains("TangibleItemId"));
    }

    [Fact]
    public void Validate_ItemReferenceExceeds100Characters_Fails()
    {
        var command = ValidCommand() with
        {
            Items = [new CreateTangibleInventoryItemRequest(Guid.NewGuid(), new string('R', 101))]
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains("Reference"));
    }

    [Fact]
    public void Validate_ItemNullReference_Passes()
    {
        var command = ValidCommand() with
        {
            Items = [new CreateTangibleInventoryItemRequest(Guid.NewGuid(), null)]
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    private static CreateTangibleInventoryCommand ValidCommand() =>
        new(
            ReportNo: "TI-2025-01-0001",
            Date: DateOnly.FromDateTime(DateTime.UtcNow),
            ReceivedFrom: "DBM Depot",
            Address: "Manila",
            ReceiptType: ReceiptType.Purchase,
            OtherReceiptType: null,
            FundCluster: "101",
            ReceivedByEmployeeId: Guid.NewGuid(),
            NotedByEmployeeId: Guid.NewGuid(),
            Items: [new CreateTangibleInventoryItemRequest(Guid.NewGuid(), "PO-2025-001")]);
}
