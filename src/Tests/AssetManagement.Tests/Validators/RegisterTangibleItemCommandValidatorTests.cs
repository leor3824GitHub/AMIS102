using FSH.Modules.AssetManagement.Features.v1.TangibleItems.RegisterTangibleItem;
using Shouldly;
using Xunit;

namespace AssetManagement.Tests.Validators;

public sealed class RegisterTangibleItemCommandValidatorTests
{
    private readonly RegisterTangibleItemCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _sut.Validate(ValidCommand());
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_EmptyPropertyNo_Fails()
    {
        var command = ValidCommand() with { PropertyNo = string.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.PropertyNo));
    }

    [Fact]
    public void Validate_PropertyNoExceeds32Characters_Fails()
    {
        var command = ValidCommand() with { PropertyNo = new string('P', 33) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.PropertyNo));
    }

    [Fact]
    public void Validate_PropertyNoExactly32Characters_Passes()
    {
        var command = ValidCommand() with { PropertyNo = new string('P', 32) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_EmptyPropertyClass_Fails()
    {
        var command = ValidCommand() with { PropertyClass = string.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.PropertyClass));
    }

    [Fact]
    public void Validate_PropertyClassExceeds20Characters_Fails()
    {
        var command = ValidCommand() with { PropertyClass = new string('C', 21) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.PropertyClass));
    }

    [Fact]
    public void Validate_EmptyCategoryCode_Fails()
    {
        var command = ValidCommand() with { CategoryCode = string.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.CategoryCode));
    }

    [Fact]
    public void Validate_CategoryCodeExceeds20Characters_Fails()
    {
        var command = ValidCommand() with { CategoryCode = new string('C', 21) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.CategoryCode));
    }

    [Fact]
    public void Validate_EmptyItemId_Fails()
    {
        var command = ValidCommand() with { ItemId = Guid.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.ItemId));
    }

    [Fact]
    public void Validate_DefaultAcquisitionDate_Fails()
    {
        var command = ValidCommand() with { AcquisitionDate = default };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.AcquisitionDate));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_QuantityNotGreaterThanZero_Fails(int quantity)
    {
        var command = ValidCommand() with { Quantity = quantity };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Quantity));
    }

    [Fact]
    public void Validate_QuantityExceeds9999_Fails()
    {
        var command = ValidCommand() with { Quantity = 10000 };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Quantity));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(9999)]
    public void Validate_QuantityWithinValidRange_Passes(int quantity)
    {
        var command = ValidCommand() with { Quantity = quantity };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_UnitCostNotGreaterThanZero_Fails(double unitCost)
    {
        var command = ValidCommand() with { UnitCost = (decimal)unitCost };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.UnitCost));
    }

    [Fact]
    public void Validate_PositiveUnitCost_Passes()
    {
        var command = ValidCommand() with { UnitCost = 0.01m };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_RemarksExceeds500Characters_Fails()
    {
        var command = ValidCommand() with { Remarks = new string('R', 501) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Remarks));
    }

    [Fact]
    public void Validate_NullRemarks_Passes()
    {
        var command = ValidCommand() with { Remarks = null };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    private static RegisterTangibleItemCommand ValidCommand() =>
        new(
            ItemId: Guid.NewGuid(),
            PropertyNo: "PPE-2025-0001",
            PropertyClass: "PPE",
            CategoryCode: "ICT-EQ",
            AcquisitionDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Quantity: 1,
            UnitCost: 50000m,
            Remarks: null);
}
