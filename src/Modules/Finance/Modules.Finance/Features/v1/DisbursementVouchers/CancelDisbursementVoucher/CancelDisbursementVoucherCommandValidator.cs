using FluentValidation;
using FSH.Modules.Finance.Contracts.v1.DisbursementVouchers;

namespace FSH.Modules.Finance.Features.v1.DisbursementVouchers.CancelDisbursementVoucher;

public sealed class CancelDisbursementVoucherCommandValidator : AbstractValidator<CancelDisbursementVoucherCommand>
{
    public CancelDisbursementVoucherCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Remarks).NotEmpty().MaximumLength(500);
    }
}
