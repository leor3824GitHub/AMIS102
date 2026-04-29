using FluentValidation;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.AmendAnnualProcurementPlan;

public sealed class CreateUpdateAppCommandValidator : AbstractValidator<CreateUpdateAppCommand>
{
    public CreateUpdateAppCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UpdateReason).NotEmpty().MaximumLength(1000);
    }
}
