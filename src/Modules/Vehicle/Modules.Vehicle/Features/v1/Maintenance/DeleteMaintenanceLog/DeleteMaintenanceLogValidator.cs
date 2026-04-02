using FluentValidation;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.DeleteMaintenanceLog;

public sealed class DeleteMaintenanceLogValidator : AbstractValidator<DeleteMaintenanceLogCommand>
{
    public DeleteMaintenanceLogValidator()
    {
        RuleFor(x => x.LogId).NotEmpty();
    }
}