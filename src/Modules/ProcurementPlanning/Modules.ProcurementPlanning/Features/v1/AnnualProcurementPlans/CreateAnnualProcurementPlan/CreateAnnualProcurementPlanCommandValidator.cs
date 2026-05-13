using FluentValidation;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.CreateAnnualProcurementPlan;

public sealed class CreateAnnualProcurementPlanCommandValidator : AbstractValidator<CreateAnnualProcurementPlanCommand>
{
    public CreateAnnualProcurementPlanCommandValidator()
    {
        RuleFor(x => x.FiscalYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Phase)
            .Must(p => p == AppPhase.Indicative)
            .WithMessage("Only Indicative APPs can be created directly. Use 'Promote to Final' to create a Final APP from an approved Indicative one.");
    }
}
