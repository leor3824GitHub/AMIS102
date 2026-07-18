using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;
using FluentValidation;

namespace AMIS.Modules.AssetRegister.Features.v1.Transfers.AcceptTransferOffer;

public sealed class AcceptTransferOfferCommandValidator : AbstractValidator<AcceptTransferOfferCommand>
{
    public AcceptTransferOfferCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ReceivingReportId)
            .NotEmpty()
            .WithMessage("Post the receiving report (PPERR) first, then link it by accepting the offer.");
    }
}
