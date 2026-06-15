using FluentValidation;
using AMIS.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;

namespace AMIS.Modules.Finance.Features.v1.BudgetUtilizationRecords.UtilizeBudgetUtilizationRecord;

public sealed class UtilizeBudgetUtilizationRecordCommandValidator : AbstractValidator<UtilizeBudgetUtilizationRecordCommand>
{
    public UtilizeBudgetUtilizationRecordCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DisbursementVoucherId).NotEmpty();
        RuleFor(x => x.DisbursementVoucherNumber).NotEmpty().MaximumLength(50);
    }
}
