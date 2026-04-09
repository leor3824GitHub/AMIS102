using FluentValidation;
using FSH.Modules.Finance.Contracts.v1.DisbursementVouchers;

namespace FSH.Modules.Finance.Features.v1.DisbursementVouchers.PayDisbursementVoucher;

public sealed class PayDisbursementVoucherCommandValidator : AbstractValidator<PayDisbursementVoucherCommand>
{
    public PayDisbursementVoucherCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}
