using FluentValidation;
using FSH.Modules.AssetRegister.Contracts.v1.Incidents;

namespace FSH.Modules.AssetRegister.Features.v1.Incidents.NotifyIncidentPolice;

public sealed class NotifyIncidentPoliceCommandValidator : AbstractValidator<NotifyIncidentPoliceCommand>
{
    public NotifyIncidentPoliceCommandValidator()
    {
        RuleFor(x => x.IncidentReportId).NotEmpty().WithMessage("Incident report ID is required.");
        RuleFor(x => x.Station).NotEmpty().MaximumLength(256).WithMessage("Police station must be provided and not exceed 256 characters.");
        RuleFor(x => x.BlotterRef).NotEmpty().MaximumLength(128).WithMessage("Blotter reference must be provided and not exceed 128 characters.");
        RuleFor(x => x.NotifiedOn).NotEqual(default(DateOnly)).WithMessage("Notification date must be provided.");
    }
}
