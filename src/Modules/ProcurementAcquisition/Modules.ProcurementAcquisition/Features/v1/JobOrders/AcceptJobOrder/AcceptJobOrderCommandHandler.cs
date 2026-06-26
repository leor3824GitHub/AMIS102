using AMIS.Framework.Core.Context;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.CreateJobOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.AcceptJobOrder;

public sealed class AcceptJobOrderCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator) : ICommandHandler<AcceptJobOrderCommand, JobOrderDto>
{
    public async ValueTask<JobOrderDto> Handle(AcceptJobOrderCommand command, CancellationToken cancellationToken)
    {
        var jo = await dbContext.JobOrders
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new AMIS.Framework.Core.Exceptions.NotFoundException($"Job order '{command.Id}' not found.");

        // The Supply Officer signatory is resolved from the authenticated identity, not the request body.
        var acceptor = await SignatoryResolver.ResolveSignatoryAsync(currentUser, mediator, cancellationToken).ConfigureAwait(false);

        jo.Accept(
            currentUser.GetUserId(),
            acceptor.Name,
            acceptor.Designation,
            command.InvoiceNo,
            command.DateReceived,
            command.IsCompleteDelivery,
            command.PartialDeliveryNote);

        jo.LastModifiedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreateJobOrderCommandHandler.MapToDto(jo);
    }
}
