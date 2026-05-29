using AMIS.Framework.Core.Context;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Domain.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CreatePurchaseRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.UpdatePurchaseRequest;

public sealed class UpdatePurchaseRequestCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator) : ICommandHandler<UpdatePurchaseRequestCommand, PurchaseRequestDto>
{
    public async ValueTask<PurchaseRequestDto> Handle(UpdatePurchaseRequestCommand command, CancellationToken cancellationToken)
    {
        var pr = await dbContext.PurchaseRequests
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new AMIS.Framework.Core.Exceptions.NotFoundException($"Purchase request '{command.Id}' not found.");

        var lineItems = command.LineItems.Select(li =>
            new PurchaseRequestLineItemData(li.Quantity, li.UnitOfIssue, li.ItemDescription, li.EstimatedUnitCost, li.CatalogItemId, li.UacsObjectCode));

        // Re-freeze the requester snapshot from the selected employee.
        var requester = await SignatoryResolver
            .ResolveByEmployeeIdAsync(command.RequestedById, mediator, cancellationToken)
            .ConfigureAwait(false);

        pr.Update(
            command.DepartmentId,
            command.ResponsibilityCenterCode,
            command.Purpose,
            command.PrType,
            command.Justification,
            requester.Name,
            command.SaiNumber,
            command.SaiDate,
            command.AlobsNumber,
            command.AlobsDate,
            lineItems,
            requestedById: command.RequestedById,
            requestedByDesignation: requester.Designation);

        pr.LastModifiedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreatePurchaseRequestCommandHandler.MapToDto(pr);
    }
}

