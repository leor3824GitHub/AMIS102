using FluentValidation;

namespace FSH.Modules.AssetManagement.Features.v1.PropertyIncidentReports.CreatePropertyIncidentReport;

public sealed class CreatePropertyIncidentReportCommandValidator
    : AbstractValidator<CreatePropertyIncidentReportCommand>
{
    public CreatePropertyIncidentReportCommandValidator()
    {
        RuleFor(x => x.ReportNo)
            .NotEmpty()
            .MaximumLength(32)
            .Matches(@"^RLS-\d{4}-\d{2}-\d{4}$")
            .WithMessage("ReportNo must follow the format RLS-YYYY-MM-NNNN (e.g. RLS-2024-01-0001).");

        RuleFor(x => x.Date)
            .NotEmpty();

        RuleFor(x => x.IncidentDate)
            .LessThanOrEqualTo(x => x.Date)
            .When(x => x.IncidentDate.HasValue)
            .WithMessage("IncidentDate must not be after the report Date.");

        RuleFor(x => x.FundCluster)
            .MaximumLength(50);

        RuleFor(x => x.IncidentDetails)
            .NotEmpty()
            .MaximumLength(1000);

        RuleFor(x => x.Remarks)
            .MaximumLength(500);

        RuleFor(x => x.TangibleInventoryItemIds)
            .NotEmpty()
            .WithMessage("At least one property must be listed.");

        RuleForEach(x => x.TangibleInventoryItemIds)
            .NotEmpty();
    }
}
