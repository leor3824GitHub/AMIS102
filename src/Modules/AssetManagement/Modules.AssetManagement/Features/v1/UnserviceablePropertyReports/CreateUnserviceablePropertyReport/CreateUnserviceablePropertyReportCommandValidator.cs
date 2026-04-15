using FluentValidation;

namespace FSH.Modules.AssetManagement.Features.v1.UnserviceablePropertyReports.CreateUnserviceablePropertyReport;

public sealed class CreateUnserviceablePropertyReportCommandValidator
    : AbstractValidator<CreateUnserviceablePropertyReportCommand>
{
    public CreateUnserviceablePropertyReportCommandValidator()
    {
        RuleFor(x => x.ReportNo)
            .NotEmpty()
            .MaximumLength(32)
            .Matches(@"^IUR-\d{4}-\d{2}-\d{4}$")
            .WithMessage("ReportNo must follow the format IUR-YYYY-MM-NNNN (e.g. IUR-2024-01-0001).");

        RuleFor(x => x.Date)
            .NotEmpty();

        RuleFor(x => x.FundCluster)
            .MaximumLength(50);

        RuleFor(x => x.Remarks)
            .MaximumLength(500);

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one property must be listed.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.SemiExpendablePropertyId).NotEmpty();
            item.RuleFor(x => x.ConditionRemarks).MaximumLength(500);
        });
    }
}
