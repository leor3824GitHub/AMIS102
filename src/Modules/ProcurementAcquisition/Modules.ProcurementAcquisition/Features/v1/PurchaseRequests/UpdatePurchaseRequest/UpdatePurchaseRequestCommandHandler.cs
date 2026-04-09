using FSH.Framework.Core.Context;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using FSH.Modules.ProcurementAcquisition.Data;
using FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CreatePurchaseRequest;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.UpdatePurchaseRequest;

public sealed class UpdatePurchaseRequestCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<UpdatePurchaseRequestCommand, PurchaseRequestDto>
{
    public async ValueTask<PurchaseRequestDto> Handle(UpdatePurchaseRequestCommand command, CancellationToken cancellationToken)
    {
        var pr = await dbContext.PurchaseRequests
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Purchase request '{command.Id}' not found.");

        var lineItems = command.LineItems.Select(li =>
            (li.Quantity, li.UnitOfIssue, li.ItemDescription, li.EstimatedUnitCost));

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
            lineItems);

        pr.LastModifiedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreatePurchaseRequestCommandHandler.MapToDto(pr);
    }
}
