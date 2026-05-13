using FluentValidation;
using AMIS.Modules.AssetManagement.Domain;

namespace AMIS.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.RenewICS;

public sealed class RenewICSCommandValidator : AbstractValidator<RenewICSCommand>
{
    public RenewICSCommandValidator()
    {
        RuleFor(x => x.OldICSId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();

        RuleFor(x => x.NewICSNo)
            .NotEmpty()
            .MaximumLength(32)
            .Must(no => no.StartsWith("SPLV-", StringComparison.Ordinal)
                     || no.StartsWith("SPHV-", StringComparison.Ordinal))
            .WithMessage("ICS number must start with SPLV- or SPHV-.");

        // SPLV-YYYY-MM-NNNN  or  SPHV-YYYY-MM-NNNN
        RuleFor(x => x.NewICSNo)
            .Matches(@"^(SPLV|SPHV)-\d{4}-\d{2}-\d{4}$")
            .WithMessage("ICS number must match the format SPLV-YYYY-MM-NNNN or SPHV-YYYY-MM-NNNN.");
    }
}

