using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Transfers.RejectTransferOffer;

/// <summary>
/// Declines an incoming transfer offer. No asset is created on this agency's books.
/// <para>
/// The sending agency's assets stay <c>TransferredOut</c> — its PPEIR is already an approved accountable
/// document, exactly as with the paper process. Bringing them back onto the sender's books is a separate
/// receiving document on their side, not an automatic reversal.
/// </para>
/// </summary>
public sealed class RejectTransferOfferCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<RejectTransferOfferCommand, AssetTransferOfferDto>
{
    public async ValueTask<AssetTransferOfferDto> Handle(
        RejectTransferOfferCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var offer = await db.AssetTransferOffers
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == cmd.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Transfer offer '{cmd.Id}' was not found.");

        if (offer.Direction != TransferOfferDirection.Inbound)
            throw new CustomException(
                "Only an incoming transfer offer can be rejected. This is your own outgoing offer.",
                [], System.Net.HttpStatusCode.UnprocessableEntity);

        offer.Reject(cmd.Reason);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return TransferMapper.ToDto(offer);
    }
}
