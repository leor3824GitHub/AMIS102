using FluentValidation;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.CreateBudgetUtilizationRequest;

public sealed class CreateBudgetUtilizationRequestCommandValidator : AbstractValidator<CreateBudgetUtilizationRequestCommand>
{
    public CreateBudgetUtilizationRequestCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.PurchaseOrderNumber).NotEmpty().MaximumLength(32);
        RuleFor(x => x.FundCluster).NotEmpty().MaximumLength(16);
        RuleFor(x => x.AllotmentClass).NotEmpty().MaximumLength(16);
        RuleFor(x => x.UacsObjectCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.ResponsibilityCenter).MaximumLength(32);
        RuleFor(x => x.Particulars).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

