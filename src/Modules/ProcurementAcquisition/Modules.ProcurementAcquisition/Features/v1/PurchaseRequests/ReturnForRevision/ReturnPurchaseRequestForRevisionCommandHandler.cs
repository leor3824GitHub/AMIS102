using AMIS.Framework.Core.Context;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CreatePurchaseRequest;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.ReturnForRevision;

public sealed class ReturnPurchaseRequestForRevisionCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<ReturnPurchaseRequestForRevisionCommand, PurchaseRequestDto>
{
    public async ValueTask<PurchaseRequestDto> Handle(ReturnPurchaseRequestForRevisionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var pr = await dbContext.PurchaseRequests
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Purchase request '{command.Id}' not found.");

        var actorId = currentUser.GetUserId();
        pr.ReturnForRevision(actorId, command.ReturnedByName, command.Reason);
        pr.LastModifiedBy = actorId.ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreatePurchaseRequestCommandHandler.MapToDto(pr);
    }
}
