using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using FSH.Modules.Vehicle.Features.v1.Vehicles.CreateVehicle;
using Shouldly;
using Xunit;

namespace Vehicle.Tests.Validators;

public sealed class CreateVehicleCommandValidatorTests
{
    private readonly CreateVehicleCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var command = new CreateVehicleCommand(
            "ABC-123",
            "Toyota",
            "Corolla",
            DateTime.UtcNow.Year,
            "Sedan",
            12000,
            "Well maintained");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_InvalidVehicleType_Fails()
    {
        var command = new CreateVehicleCommand(
            "ABC-123",
            "Toyota",
            "Corolla",
            DateTime.UtcNow.Year,
            "Boat",
            12000,
            null);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(command.Type));
    }
}