using FluentValidation;
using FSH.Modules.AssetManagement.Domain;

namespace FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.CreateICS;

public sealed class CreateICSCommandValidator : AbstractValidator<CreateICSCommand>
{
    public CreateICSCommandValidator()
    {
        RuleFor(x => x.ICSNo).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Category).IsInEnum().WithMessage("Category must be a valid AssetCategory value.");

        // Enforce COA number format: SPLV-YYYY-MM-NNNN for low-valued, SPHV-YYYY-MM-NNNN for high-valued.
        // Case-sensitive — COA document numbers are always uppercase.
        RuleFor(x => x.ICSNo)
            .Must((cmd, icsNo) => icsNo.StartsWith("SPLV-", StringComparison.Ordinal))
            .When(x => x.Category == AssetCategory.LowValuedSemi)
            .WithMessage("Low-valued ICS numbers must start with 'SPLV-' (e.g., SPLV-2026-04-0001).");

        RuleFor(x => x.ICSNo)
            .Must((cmd, icsNo) => icsNo.StartsWith("SPHV-", StringComparison.Ordinal))
            .When(x => x.Category == AssetCategory.HighValuedSemi)
            .WithMessage("High-valued ICS numbers must start with 'SPHV-' (e.g., SPHV-2026-04-0001).");

        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.FundCluster).MaximumLength(50);
        RuleFor(x => x.ReceivedByEmployeeId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one item is required.");
        RuleForEach(x => x.Items).SetValidator(new CreateICSItemRequestValidator());
    }
}

internal sealed class CreateICSItemRequestValidator : AbstractValidator<CreateICSItemRequest>
{
    public CreateICSItemRequestValidator()
    {
        RuleFor(x => x.SemiExpendablePropertyId).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
