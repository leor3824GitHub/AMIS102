using FluentValidation;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;

namespace AMIS.Modules.Vehicle.Features.v1.Vehicles.AssignVehicle;

public sealed class AssignVehicleCommandValidator : AbstractValidator<AssignVehicleCommand>
{
    public AssignVehicleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DepartmentName).MaximumLength(255).When(x => x.DepartmentName != null);
        RuleFor(x => x.DriverName).MaximumLength(255).When(x => x.DriverName != null);

        RuleFor(x => x)
            .Must(x => HaveMatchingPair(x.DepartmentId, x.DepartmentName))
            .WithMessage("Department ID and name must both be provided or both omitted.");

        RuleFor(x => x)
            .Must(x => HaveMatchingPair(x.DriverId, x.DriverName))
            .WithMessage("Driver ID and name must both be provided or both omitted.");
    }

    private static bool HaveMatchingPair(Guid? id, string? name) =>
        id.HasValue == !string.IsNullOrWhiteSpace(name);
}

