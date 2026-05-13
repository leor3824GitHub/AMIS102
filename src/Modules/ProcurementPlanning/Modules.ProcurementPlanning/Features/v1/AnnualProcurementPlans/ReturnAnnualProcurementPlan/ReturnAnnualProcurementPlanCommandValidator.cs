using FluentValidation;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ReturnAnnualProcurementPlan;

public sealed class ReturnAnnualProcurementPlanCommandValidator : AbstractValidator<ReturnAppCommand>
{
    public ReturnAnnualProcurementPlanCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ReturnReason).NotEmpty().MaximumLength(1000);
    }
}

