using FluentValidation;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ApproveAnnualProcurementPlan;

public sealed class ApproveAnnualProcurementPlanCommandValidator : AbstractValidator<ApproveAppCommand>
{
    public ApproveAnnualProcurementPlanCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApprovedById).NotEmpty();
    }
}
