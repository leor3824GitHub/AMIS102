using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1.Incidents;

namespace AMIS.Modules.AssetRegister.Features.v1.Incidents.NotarizeIncidentReport;

public sealed class NotarizeIncidentReportCommandValidator : AbstractValidator<NotarizeIncidentReportCommand>
{
    public NotarizeIncidentReportCommandValidator()
    {
        RuleFor(x => x.IncidentReportId).NotEmpty().WithMessage("Incident report ID is required.");
        RuleFor(x => x.NotarizedOn).NotEqual(default(DateOnly)).WithMessage("Notarization date must be provided.");
        RuleFor(x => x.DocNo).NotEmpty().MaximumLength(64).WithMessage("Document number must be provided and not exceed 64 characters.");
        RuleFor(x => x.PageNo).NotEmpty().MaximumLength(64).WithMessage("Page number must be provided and not exceed 64 characters.");
        RuleFor(x => x.BookNo).NotEmpty().MaximumLength(64).WithMessage("Book number must be provided and not exceed 64 characters.");
        RuleFor(x => x.SeriesOf).NotEmpty().MaximumLength(64).WithMessage("Series of must be provided and not exceed 64 characters.");
    }
}

