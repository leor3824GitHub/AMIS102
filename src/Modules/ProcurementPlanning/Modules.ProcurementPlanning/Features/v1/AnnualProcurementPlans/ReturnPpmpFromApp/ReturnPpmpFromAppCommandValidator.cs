using FluentValidation;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ReturnPpmpFromApp;

public sealed class ReturnPpmpFromAppCommandValidator : AbstractValidator<ReturnPpmpFromAppCommand>
{
    public ReturnPpmpFromAppCommandValidator()
    {
        RuleFor(x => x.AppId).NotEmpty();
        RuleFor(x => x.PpmpId).NotEmpty();
        RuleFor(x => x.ReturnReason).NotEmpty().MaximumLength(1000);
    }
}
