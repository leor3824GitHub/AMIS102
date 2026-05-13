using FluentValidation;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.LogMaintenanceCompletion;

public sealed class LogMaintenanceCompletionValidator : AbstractValidator<LogMaintenanceCompletionCommand>
{
    public LogMaintenanceCompletionValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.MaintenanceType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PerformedDate).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).When(x => x.Cost.HasValue);
    }
}

