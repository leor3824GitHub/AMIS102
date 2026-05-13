using FluentValidation;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.CreateUpdateApp;

public sealed class CreateUpdateAppCommandValidator : AbstractValidator<CreateUpdateAppCommand>
{
    public CreateUpdateAppCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UpdateReason).NotEmpty().MaximumLength(1000);
    }
}

