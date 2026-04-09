using FluentValidation;
using FSH.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;

namespace FSH.Modules.Finance.Features.v1.BudgetUtilizationRecords.CancelBudgetUtilizationRecord;

public sealed class CancelBudgetUtilizationRecordCommandValidator : AbstractValidator<CancelBudgetUtilizationRecordCommand>
{
    public CancelBudgetUtilizationRecordCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Remarks).NotEmpty().MaximumLength(500);
    }
}
