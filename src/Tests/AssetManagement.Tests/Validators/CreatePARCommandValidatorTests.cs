using FSH.Modules.AssetManagement.Domain;
using FSH.Modules.AssetManagement.Features.v1.PropertyAcknowledgementReceipts.CreatePAR;
using Shouldly;
using Xunit;

namespace AssetManagement.Tests.Validators;

public sealed class CreatePARCommandValidatorTests
{
    private readonly CreatePARCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _sut.Validate(ValidCommand());
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_EmptyPARNo_Fails()
    {
        var command = ValidCommand() with { PARNo = string.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.PARNo));
    }

    [Fact]
    public void Validate_PARNoExceeds32Characters_Fails()
    {
        var command = ValidCommand() with { PARNo = new string('P', 33) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.PARNo));
    }

    [Fact]
    public void Validate_PARNoExactly32Characters_Passes()
    {
        var command = ValidCommand() with { PARNo = new string('P', 32) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_DefaultDate_Fails()
    {
        var command = ValidCommand() with { Date = default };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Date));
    }

    [Theory]
    [InlineData(PARType.NewPurchase)]
    [InlineData(PARType.Transfer)]
    public void Validate_ValidPARType_Passes(PARType parType)
    {
        var command = ValidCommand() with { PARType = parType };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_InvalidPARType_Fails()
    {
        var command = ValidCommand() with { PARType = (PARType)99 };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.PARType));
    }

    [Fact]
    public void Validate_EmptyReceivedFromEmployeeId_Fails()
    {
        var command = ValidCommand() with { ReceivedFromEmployeeId = Guid.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.ReceivedFromEmployeeId));
    }

    [Fact]
    public void Validate_EmptyReceivedByEmployeeId_Fails()
    {
        var command = ValidCommand() with { ReceivedByEmployeeId = Guid.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.ReceivedByEmployeeId));
    }

    [Fact]
    public void Validate_EmptyApprovedByEmployeeId_Fails()
    {
        var command = ValidCommand() with { ApprovedByEmployeeId = Guid.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.ApprovedByEmployeeId));
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
    public void Validate_ItemWithEmptyTangibleInventoryItemId_Fails()
    {
        var command = ValidCommand() with
        {
            Items = [new CreatePARItemRequest(Guid.Empty, 1, "unit", "description")]
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains("TangibleInventoryItemId"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ItemQuantityNotGreaterThanZero_Fails(int quantity)
    {
        var command = ValidCommand() with
        {
            Items = [new CreatePARItemRequest(Guid.NewGuid(), quantity, "unit", "description")]
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains("Quantity"));
    }

    [Fact]
    public void Validate_ItemWithEmptyUnit_Fails()
    {
        var command = ValidCommand() with
        {
            Items = [new CreatePARItemRequest(Guid.NewGuid(), 1, string.Empty, "description")]
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains("Unit"));
    }

    [Fact]
    public void Validate_ItemUnitExceeds32Characters_Fails()
    {
        var command = ValidCommand() with
        {
            Items = [new CreatePARItemRequest(Guid.NewGuid(), 1, new string('U', 33), "description")]
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains("Unit"));
    }

    [Fact]
    public void Validate_ItemWithEmptyItemDescription_Fails()
    {
        var command = ValidCommand() with
        {
            Items = [new CreatePARItemRequest(Guid.NewGuid(), 1, "unit", string.Empty)]
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains("ItemDescription"));
    }

    [Fact]
    public void Validate_ItemDescriptionExceeds500Characters_Fails()
    {
        var command = ValidCommand() with
        {
            Items = [new CreatePARItemRequest(Guid.NewGuid(), 1, "unit", new string('D', 501))]
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains("ItemDescription"));
    }

    [Fact]
    public void Validate_MultipleValidItems_Passes()
    {
        var command = ValidCommand() with
        {
            Items =
            [
                new CreatePARItemRequest(Guid.NewGuid(), 1, "unit", "Item one"),
                new CreatePARItemRequest(Guid.NewGuid(), 5, "pcs", "Item two"),
            ]
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    private static CreatePARCommand ValidCommand() =>
        new(
            PARNo: "PAR-2025-01-0001",
            Date: DateOnly.FromDateTime(DateTime.UtcNow),
            PARType: PARType.NewPurchase,
            ReceivedFromEmployeeId: Guid.NewGuid(),
            ReceivedByEmployeeId: Guid.NewGuid(),
            ApprovedByEmployeeId: Guid.NewGuid(),
            Items: [new CreatePARItemRequest(Guid.NewGuid(), 1, "unit", "Laptop computer")]);
}
