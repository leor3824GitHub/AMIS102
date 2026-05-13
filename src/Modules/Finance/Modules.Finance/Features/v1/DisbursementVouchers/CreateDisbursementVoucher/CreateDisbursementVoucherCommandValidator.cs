using FluentValidation;
using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;

namespace AMIS.Modules.Finance.Features.v1.DisbursementVouchers.CreateDisbursementVoucher;

public sealed class CreateDisbursementVoucherCommandValidator : AbstractValidator<CreateDisbursementVoucherCommand>
{
    public CreateDisbursementVoucherCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.PurchaseOrderNumber).NotEmpty().MaximumLength(32);
        RuleFor(x => x.FundCluster).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Payee).NotEmpty().MaximumLength(256);
        RuleFor(x => x.TinNo).MaximumLength(32);
        RuleFor(x => x.PayeeAddress).MaximumLength(500);
        RuleFor(x => x.Particulars).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.ModeOfPayment).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

