using FSH.Framework.Core.Context;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using FSH.Modules.ProcurementAcquisition.Data;
using FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CreatePurchaseRequest;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.ApprovePurchaseRequest;

public sealed class ApprovePurchaseRequestCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<ApprovePurchaseRequestCommand, PurchaseRequestDto>
{
    public async ValueTask<PurchaseRequestDto> Handle(ApprovePurchaseRequestCommand command, CancellationToken cancellationToken)
    {
        var pr = await dbContext.PurchaseRequests
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Purchase request '{command.Id}' not found.");

        pr.Approve(command.ApprovedById);
        pr.LastModifiedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreatePurchaseRequestCommandHandler.MapToDto(pr);
    }
}
