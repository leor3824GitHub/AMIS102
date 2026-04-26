using FSH.Modules.AssetProcurement.Contracts.v1.AssetPurchaseRequests;
using FSH.Modules.AssetProcurement.Data;
using FSH.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.CreateAssetPurchaseRequest;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.UpdateAssetPurchaseRequest;

public sealed class UpdateAssetPurchaseRequestCommandHandler(
    AssetProcurementDbContext dbContext) : ICommandHandler<UpdateAssetPurchaseRequestCommand, AssetPurchaseRequestDto>
{
    public async ValueTask<AssetPurchaseRequestDto> Handle(UpdateAssetPurchaseRequestCommand command, CancellationToken cancellationToken)
    {
        var pr = await dbContext.AssetPurchaseRequests
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset purchase request '{command.Id}' not found.");

        pr.Update(
            command.DepartmentId,
            command.Section,
            command.Purpose,
            command.PrType,
            command.Justification,
            command.RequestedById,
            command.SaiNumber,
            command.SaiDate,
            command.AlobsNumber,
            command.AlobsDate,
            command.LineItems);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return CreateAssetPurchaseRequestCommandHandler.MapToDto(pr);
    }
}
