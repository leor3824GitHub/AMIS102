using AMIS.Modules.AssetManagement.Domain;
using AMIS.Modules.AssetManagement.Features.v1.Locations.UpdateLocation;
using Shouldly;
using Xunit;

namespace AssetManagement.Tests.Validators;

public sealed class UpdateLocationCommandValidatorTests
{
    private readonly UpdateLocationCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _sut.Validate(ValidCommand());
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var command = ValidCommand() with { Id = Guid.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Id));
    }

    [Fact]
    public void Validate_EmptyCode_Fails()
    {
        var command = ValidCommand() with { Code = string.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Code));
    }

    [Fact]
    public void Validate_CodeExceeds64Characters_Fails()
    {
        var command = ValidCommand() with { Code = new string('C', 65) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Code));
    }

    [Fact]
    public void Validate_EmptyName_Fails()
    {
        var command = ValidCommand() with { Name = string.Empty };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Name));
    }

    [Fact]
    public void Validate_NameExceeds200Characters_Fails()
    {
        var command = ValidCommand() with { Name = new string('N', 201) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Name));
    }

    [Fact]
    public void Validate_DescriptionExceeds500Characters_Fails()
    {
        var command = ValidCommand() with { Description = new string('D', 501) };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Description));
    }

    [Fact]
    public void Validate_NullDescription_Passes()
    {
        var command = ValidCommand() with { Description = null };
        var result = _sut.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    private static UpdateLocationCommand ValidCommand() =>
        new(
            Id: Guid.NewGuid(),
            Code: "OFF-02",
            Name: "Records Office",
            Type: LocationType.Office,
            ParentLocationId: null,
            Description: "Updated description");
}

