using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Transfers.GetTransferOffer;

public sealed class GetTransferOfferQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetTransferOfferQuery, AssetTransferOfferDto?>
{
    public async ValueTask<AssetTransferOfferDto?> Handle(
        GetTransferOfferQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var offer = await db.AssetTransferOffers
            .AsNoTracking()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        return offer is null ? null : TransferMapper.ToDto(offer);
    }
}
