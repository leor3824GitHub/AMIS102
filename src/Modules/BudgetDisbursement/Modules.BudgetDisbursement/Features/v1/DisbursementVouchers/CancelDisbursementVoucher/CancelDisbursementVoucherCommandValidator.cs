using FluentValidation;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.CancelDisbursementVoucher;

public sealed class CancelDisbursementVoucherCommandValidator : AbstractValidator<CancelDisbursementVoucherCommand>
{
    public CancelDisbursementVoucherCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Remarks).NotEmpty().MaximumLength(500);
    }
}

