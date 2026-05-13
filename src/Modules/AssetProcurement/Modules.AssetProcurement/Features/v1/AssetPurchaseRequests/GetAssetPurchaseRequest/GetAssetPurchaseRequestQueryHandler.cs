using AMIS.Modules.AssetProcurement.Contracts.v1.AssetPurchaseRequests;
using AMIS.Modules.AssetProcurement.Data;
using AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.CreateAssetPurchaseRequest;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.GetAssetPurchaseRequest;

public sealed class GetAssetPurchaseRequestQueryHandler(
    AssetProcurementDbContext dbContext) : IQueryHandler<GetAssetPurchaseRequestQuery, AssetPurchaseRequestDto?>
{
    public async ValueTask<AssetPurchaseRequestDto?> Handle(GetAssetPurchaseRequestQuery query, CancellationToken cancellationToken)
    {
        var pr = await dbContext.AssetPurchaseRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        return pr is null ? null : CreateAssetPurchaseRequestCommandHandler.MapToDto(pr);
    }
}

