using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;
using FluentValidation;

namespace AMIS.Modules.AssetRegister.Features.v1.Transfers.RejectTransferOffer;

public sealed class RejectTransferOfferCommandValidator : AbstractValidator<RejectTransferOfferCommand>
{
    public RejectTransferOfferCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required so the sending agency knows why the transfer was declined.")
            .MaximumLength(1000);
    }
}
