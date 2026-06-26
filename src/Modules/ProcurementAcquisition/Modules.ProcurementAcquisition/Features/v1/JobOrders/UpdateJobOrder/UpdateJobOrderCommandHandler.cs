using AMIS.Framework.Core.Context;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Domain.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.CreateJobOrder;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.UpdateJobOrder;

public sealed class UpdateJobOrderCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<UpdateJobOrderCommand, JobOrderDto>
{
    public async ValueTask<JobOrderDto> Handle(UpdateJobOrderCommand command, CancellationToken cancellationToken)
    {
        var jo = await dbContext.JobOrders
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new AMIS.Framework.Core.Exceptions.NotFoundException($"Job order '{command.Id}' not found.");

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
            lineItems);

        jo.LastModifiedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreateJobOrderCommandHandler.MapToDto(jo);
    }
}
