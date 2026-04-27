using FluentValidation;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.AmendAnnualProcurementPlan;

public sealed class AmendAnnualProcurementPlanCommandValidator : AbstractValidator<AmendAnnualProcurementPlanCommand>
{
    public AmendAnnualProcurementPlanCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.AmendmentReason).NotEmpty().MaximumLength(1000);
    }
}
