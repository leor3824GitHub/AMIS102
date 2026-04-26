using FSH.Modules.AssetProcurement.Contracts.v1.AssetPurchaseRequests;
using FSH.Modules.AssetProcurement.Data;
using FSH.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.CreateAssetPurchaseRequest;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.ApproveAssetPurchaseRequest;

public sealed class ApproveAssetPurchaseRequestCommandHandler(
    AssetProcurementDbContext dbContext) : ICommandHandler<ApproveAssetPurchaseRequestCommand, AssetPurchaseRequestDto>
{
    public async ValueTask<AssetPurchaseRequestDto> Handle(ApproveAssetPurchaseRequestCommand command, CancellationToken cancellationToken)
    {
        var pr = await dbContext.AssetPurchaseRequests
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset purchase request '{command.Id}' not found.");

        pr.Approve(command.ApprovedById);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreateAssetPurchaseRequestCommandHandler.MapToDto(pr);
    }
}
