using FluentValidation;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;

namespace AMIS.Modules.Vehicle.Features.v1.Vehicles.UpdateOdometer;

public sealed class UpdateOdometerCommandValidator : AbstractValidator<UpdateOdometerCommand>
{
    public UpdateOdometerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reading).GreaterThanOrEqualTo(0);
    }
}

