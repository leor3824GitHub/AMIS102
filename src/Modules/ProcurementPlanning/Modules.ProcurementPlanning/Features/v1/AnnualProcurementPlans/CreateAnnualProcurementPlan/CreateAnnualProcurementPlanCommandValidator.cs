using FluentValidation;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.CreateAnnualProcurementPlan;

public sealed class CreateAnnualProcurementPlanCommandValidator : AbstractValidator<CreateAnnualProcurementPlanCommand>
{
    public CreateAnnualProcurementPlanCommandValidator()
    {
        RuleFor(x => x.FiscalYear).InclusiveBetween(2000, 2100);
    }
}
