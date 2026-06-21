using FluentValidation;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.UtilizeBudgetUtilizationRequest;

public sealed class UtilizeBudgetUtilizationRequestCommandValidator : AbstractValidator<UtilizeBudgetUtilizationRequestCommand>
{
    public UtilizeBudgetUtilizationRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DisbursementVoucherId).NotEmpty();
        RuleFor(x => x.DisbursementVoucherNumber).NotEmpty().MaximumLength(50);
    }
}
