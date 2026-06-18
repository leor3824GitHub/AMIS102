using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;

namespace AMIS.Modules.Finance.Domain.DisbursementVouchers;

/// <summary>
/// A single configurable deduction line on a <see cref="DisbursementVoucher"/> (e.g. "5% Withholding
/// Tax", "1% Tax", or a flat fee). Owned by the voucher aggregate — it has no independent lifecycle.
/// <see cref="Value"/> is interpreted per <see cref="Type"/>: a percentage of the voucher's gross
/// amount, or a fixed peso figure deducted as-is.
/// </summary>
public sealed class DvDeduction
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public DvDeductionType Type { get; private set; }
    public decimal Value { get; private set; }

    private DvDeduction() { }

    public static DvDeduction Create(string name, DvDeductionType type, decimal value) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Value = value
        };

    /// <summary>Resolves this line to a peso amount against the voucher's gross amount.</summary>
    public decimal ComputeAmount(decimal grossAmount) =>
        Type == DvDeductionType.Percentage
            ? Math.Round(grossAmount * (Value / 100m), 2, MidpointRounding.AwayFromZero)
            : Value;
}
