using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.BudgetDisbursement.Domain.DisbursementVouchers;
using FluentValidation;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers;

/// <summary>Validates a single deduction line. A percentage line must be 0&lt;rate≤100; a fixed line a
/// positive peso figure. Shared by the Create and Update voucher validators.</summary>
public sealed class DvDeductionInputValidator : AbstractValidator<DvDeductionInput>
{
    public DvDeductionInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.Value)
            .LessThanOrEqualTo(100)
            .When(x => x.Type == DvDeductionType.Percentage)
            .WithMessage("Percentage deduction must be between 0 and 100.");
    }
}

internal static class DvDeductionRules
{
    /// <summary>True when the deduction lines resolve to a total that does not exceed the gross amount.</summary>
    public static bool TotalWithinAmount(IReadOnlyList<DvDeductionInput>? deductions, decimal amount)
    {
        if (deductions is null || deductions.Count == 0)
            return true;

        var total = deductions.Sum(d => DvDeduction.Create(d.Name, d.Type, d.Value).ComputeAmount(amount));
        return total <= amount;
    }
}
