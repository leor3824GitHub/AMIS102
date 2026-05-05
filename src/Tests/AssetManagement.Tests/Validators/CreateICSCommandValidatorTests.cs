using FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.CreateICS;
using Shouldly;
using Xunit;

namespace AssetManagement.Tests.Validators;

public sealed class CreateICSCommandValidatorTests
{
    private readonly CreateICSCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _sut.Validate(ValidCommand());
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_EmptyICSNo_Fails()
    {
        var command = ValidCommand() with { ICSNo = string.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.ICSNo));
    }

    [Fact]
    public void Validate_ICSNoExceeds32Characters_Fails()
    {
        var command = ValidCommand() with { ICSNo = new string('A', 33) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.ICSNo));
    }

    [Fact]
    public void Validate_ICSNoExactly32Characters_Passes()
    {
        var command = ValidCommand() with { ICSNo = new string('A', 32) };
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

    [Fact]
    public void Validate_FundClusterExceeds50Characters_Fails()
    {
        var command = ValidCommand() with { FundCluster = new string('X', 51) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.FundCluster));
    }

    [Fact]
    public void Validate_NullFundCluster_Passes()
    {
        var command = ValidCommand() with { FundCluster = null };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
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
            Items = [new CreateICSItemRequest(Guid.Empty, null)]
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains("TangibleInventoryItemId"));
    }

    [Fact]
    public void Validate_ItemDescriptionExceeds500Characters_Fails()
    {
        var command = ValidCommand() with
        {
            Items = [new CreateICSItemRequest(Guid.NewGuid(), new string('D', 501))]
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains("Description"));
    }

    [Fact]
    public void Validate_ItemDescriptionNull_Passes()
    {
        var command = ValidCommand() with
        {
            Items = [new CreateICSItemRequest(Guid.NewGuid(), null)]
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_MultipleItems_Passes()
    {
        var command = ValidCommand() with
        {
            Items =
            [
                new CreateICSItemRequest(Guid.NewGuid(), "Item 1"),
                new CreateICSItemRequest(Guid.NewGuid(), "Item 2"),
            ]
        };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    private static CreateICSCommand ValidCommand() =>
        new(
            ICSNo: "ICS-2025-01-0001",
            Date: DateOnly.FromDateTime(DateTime.UtcNow),
            FundCluster: "101",
            IssuedFromEmployeeId: Guid.NewGuid(),
            ReceivedByEmployeeId: Guid.NewGuid(),
            Items: [new CreateICSItemRequest(Guid.NewGuid(), "Laptop")]);
}
