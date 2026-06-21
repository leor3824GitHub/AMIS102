using FluentValidation;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.CancelBudgetUtilizationRequest;

public sealed class CancelBudgetUtilizationRequestCommandValidator : AbstractValidator<CancelBudgetUtilizationRequestCommand>
{
    public CancelBudgetUtilizationRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Remarks).NotEmpty().MaximumLength(500);
    }
}

