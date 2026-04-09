using FluentValidation;
using FSH.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;

namespace FSH.Modules.Finance.Features.v1.BudgetUtilizationRecords.CreateBudgetUtilizationRecord;

public sealed class CreateBudgetUtilizationRecordCommandValidator : AbstractValidator<CreateBudgetUtilizationRecordCommand>
{
    public CreateBudgetUtilizationRecordCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.PurchaseOrderNumber).NotEmpty().MaximumLength(32);
        RuleFor(x => x.DisbursementVoucherNumber).MaximumLength(32);
        RuleFor(x => x.AllotmentClass).NotEmpty().MaximumLength(16);
        RuleFor(x => x.UacsObjectCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.ResponsibilityCenter).MaximumLength(32);
        RuleFor(x => x.Particulars).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}
