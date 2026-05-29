using AMIS.Framework.Core.Context;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CreatePurchaseRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.ApprovePurchaseRequest;

public sealed class ApprovePurchaseRequestCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator) : ICommandHandler<ApprovePurchaseRequestCommand, PurchaseRequestDto>
{
    public async ValueTask<PurchaseRequestDto> Handle(ApprovePurchaseRequestCommand command, CancellationToken cancellationToken)
    {
        var pr = await dbContext.PurchaseRequests
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new AMIS.Framework.Core.Exceptions.NotFoundException($"Purchase request '{command.Id}' not found.");

        var approverId = currentUser.GetUserId();
        // Signatory name comes from the authenticated identity, never from the request body.
        var approvedByName = await SignatoryResolver.ResolveNameAsync(currentUser, mediator, cancellationToken).ConfigureAwait(false);
        pr.Approve(approvedByName, approverId);
        pr.LastModifiedBy = approverId.ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreatePurchaseRequestCommandHandler.MapToDto(pr);
    }
}

