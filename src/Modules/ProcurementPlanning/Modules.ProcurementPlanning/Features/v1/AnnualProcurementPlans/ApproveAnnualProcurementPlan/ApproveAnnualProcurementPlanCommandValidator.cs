using FluentValidation;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ApproveAnnualProcurementPlan;

public sealed class ApproveAnnualProcurementPlanCommandValidator : AbstractValidator<ApproveAppCommand>
{
    public ApproveAnnualProcurementPlanCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

