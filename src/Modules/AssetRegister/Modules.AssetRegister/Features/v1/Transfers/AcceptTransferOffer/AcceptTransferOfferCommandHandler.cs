using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Transfers.AcceptTransferOffer;

/// <summary>
/// Links an inbound offer to the PPERR the receiving agency posted on its own form series.
/// <para>
/// The receiving report is deliberately NOT created here. The receiving agency must issue its own property
/// numbers from its own pre-numbered accountable forms, supply its own signatories, and apply its own
/// capitalization threshold (which decides PPE vs Semi-Expendable and can differ per agency). So the user
/// posts the PPERR through the ordinary CreateReceivingReport flow first — prefilled from this offer — and
/// this command only records the link and flips the offer to Accepted.
/// </para>
/// <para>
/// The sending agency learns of the acceptance when <c>AssetTransferProjectionJob</c> carries the response
/// back on its next pass. Nothing here writes across the tenant boundary.
/// </para>
/// </summary>
public sealed class AcceptTransferOfferCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<AcceptTransferOfferCommand, AssetTransferOfferDto>
{
    public async ValueTask<AssetTransferOfferDto> Handle(
        AcceptTransferOfferCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var offer = await db.AssetTransferOffers
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == cmd.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Transfer offer '{cmd.Id}' was not found.");

        if (offer.Direction != TransferOfferDirection.Inbound)
            throw new CustomException(
                "Only an incoming transfer offer can be accepted. This is your own outgoing offer.",
                [], System.Net.HttpStatusCode.UnprocessableEntity);

        // The receiving report must be one of ours. The ambient tenant filter already guarantees that —
        // a report belonging to another agency simply won't be found.
        var report = await db.ReceivingReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == cmd.ReceivingReportId, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException(
                $"Receiving report '{cmd.ReceivingReportId}' was not found. Post the PPERR first, then accept the offer.",
                [], System.Net.HttpStatusCode.UnprocessableEntity);

        var alreadyLinked = await db.AssetTransferOffers
            .AnyAsync(o => o.Id != offer.Id && o.ReceivingReportId == cmd.ReceivingReportId, cancellationToken)
            .ConfigureAwait(false);
        if (alreadyLinked)
            throw new CustomException(
                $"Receiving report '{report.ReportNo}' is already linked to another transfer offer.",
                [], System.Net.HttpStatusCode.Conflict);

        // Domain enforces the state machine (no double-accept, no accept-after-reject).
        offer.Accept(report.Id, report.ReportNo);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return TransferMapper.ToDto(offer);
    }
}
