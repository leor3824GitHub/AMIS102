using FluentValidation;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ReturnAnnualProcurementPlan;

public sealed class ReturnAnnualProcurementPlanCommandValidator : AbstractValidator<ReturnAppCommand>
{
    public ReturnAnnualProcurementPlanCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ReturnReason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ReturnedById).NotEmpty();
    }
}
