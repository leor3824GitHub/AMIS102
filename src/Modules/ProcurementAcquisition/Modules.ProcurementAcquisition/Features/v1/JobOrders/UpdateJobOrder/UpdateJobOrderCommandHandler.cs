using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Domain.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.CreateJobOrder;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.UpdateJobOrder;

public sealed class UpdateJobOrderCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator) : ICommandHandler<UpdateJobOrderCommand, JobOrderDto>
{
    public async ValueTask<JobOrderDto> Handle(UpdateJobOrderCommand command, CancellationToken cancellationToken)
    {
        var jo = await dbContext.JobOrders
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Job order '{command.Id}' not found.");

        var inspector = command.InspectorId == Guid.Empty
            ? throw new CustomException("An inspector is required.", Enumerable.Empty<string>(), System.Net.HttpStatusCode.BadRequest)
            : await mediator.Send(new GetEmployeeReferenceByIdQuery(command.InspectorId), cancellationToken).ConfigureAwait(false)
              ?? throw new NotFoundException($"Inspector employee '{command.InspectorId}' not found.");

        var lineItems = command.LineItems
            .Select(li => new JobOrderLineItemData(li.Unit, li.Description, li.Quantity, li.UnitCost))
            .ToList();

        jo.Update(
            command.JobRequestNo,
            command.RequisitioningOffice,
            command.SupplierId,
            command.SupplierName,
            command.SupplierAddress,
            command.SupplierTin,
            command.ModeOfProcurement,
            command.PlaceOfDelivery,
            command.DateOfDelivery,
            command.DeliveryTerm,
            command.PaymentTerm,
            command.FundCluster,
            command.OursBursNumber,
            command.InspectorId,
            $"{inspector.FirstName} {inspector.LastName}".Trim(),
            inspector.PositionName,
            lineItems);

        jo.LastModifiedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreateJobOrderCommandHandler.MapToDto(jo);
    }
}
