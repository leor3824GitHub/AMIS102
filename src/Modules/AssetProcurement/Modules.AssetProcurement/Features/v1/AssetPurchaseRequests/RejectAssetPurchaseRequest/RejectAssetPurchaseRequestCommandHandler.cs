using FSH.Modules.AssetProcurement.Contracts.v1.AssetPurchaseRequests;
using FSH.Modules.AssetProcurement.Data;
using FSH.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.CreateAssetPurchaseRequest;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.RejectAssetPurchaseRequest;

public sealed class RejectAssetPurchaseRequestCommandHandler(
    AssetProcurementDbContext dbContext) : ICommandHandler<RejectAssetPurchaseRequestCommand, AssetPurchaseRequestDto>
{
    public async ValueTask<AssetPurchaseRequestDto> Handle(RejectAssetPurchaseRequestCommand command, CancellationToken cancellationToken)
    {
        var pr = await dbContext.AssetPurchaseRequests
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset purchase request '{command.Id}' not found.");

        pr.Reject(command.Reason);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreateAssetPurchaseRequestCommandHandler.MapToDto(pr);
    }
}
