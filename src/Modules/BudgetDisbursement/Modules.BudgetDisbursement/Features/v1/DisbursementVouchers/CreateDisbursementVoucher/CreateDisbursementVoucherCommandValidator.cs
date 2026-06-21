using FluentValidation;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.CreateDisbursementVoucher;

public sealed class CreateDisbursementVoucherCommandValidator : AbstractValidator<CreateDisbursementVoucherCommand>
{
    public CreateDisbursementVoucherCommandValidator()
    {
        RuleFor(x => x.BudgetUtilizationRequestId).NotEmpty();
        RuleFor(x => x.BurNumber).NotEmpty().MaximumLength(32);
        RuleFor(x => x.FundCluster).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Payee).NotEmpty().MaximumLength(256);
        RuleFor(x => x.TinNo).MaximumLength(32);
        RuleFor(x => x.PayeeAddress).MaximumLength(500);
        RuleFor(x => x.Particulars).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.ModeOfPayment).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Remarks).MaximumLength(500);

        RuleForEach(x => x.Deductions).SetValidator(new DvDeductionInputValidator());
        RuleFor(x => x)
            .Must(x => DvDeductionRules.TotalWithinAmount(x.Deductions, x.Amount))
            .WithName(nameof(CreateDisbursementVoucherCommand.Deductions))
            .WithMessage("Total deductions cannot exceed the gross amount.");
    }
}

