using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using FSH.Modules.Vehicle.Features.v1.Vehicles.AssignVehicle;
using Shouldly;
using Xunit;

namespace Vehicle.Tests.Validators;

public sealed class AssignVehicleCommandValidatorTests
{
    private readonly AssignVehicleCommandValidator _sut = new();

    [Fact]
    public void Validate_DepartmentPairMismatch_Fails()
    {
        var command = new AssignVehicleCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            null);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage.Contains("Department ID and name"));
    }

    [Fact]
    public void Validate_MatchingPairs_Passes()
    {
        var command = new AssignVehicleCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Operations",
            Guid.NewGuid(),
            "Jane Driver");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }
}