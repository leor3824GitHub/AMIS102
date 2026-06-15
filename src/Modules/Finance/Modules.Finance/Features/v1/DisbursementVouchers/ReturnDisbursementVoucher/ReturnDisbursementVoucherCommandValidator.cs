using FluentValidation;
using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;

namespace AMIS.Modules.Finance.Features.v1.DisbursementVouchers.ReturnDisbursementVoucher;

public sealed class ReturnDisbursementVoucherCommandValidator : AbstractValidator<ReturnDisbursementVoucherCommand>
{
    public ReturnDisbursementVoucherCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Remarks).NotEmpty().MaximumLength(500);
    }
}
