using FluentValidation;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.RemovePpmpFromApp;

public sealed class RemovePpmpFromAppCommandValidator : AbstractValidator<RemovePpmpFromAppCommand>
{
    public RemovePpmpFromAppCommandValidator()
    {
        RuleFor(x => x.AppId).NotEmpty();
        RuleFor(x => x.PpmpId).NotEmpty();
    }
}
