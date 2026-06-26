using AMIS.Framework.Core.Context;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.CreateJobOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.InspectJobOrder;

public sealed class InspectJobOrderCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator) : ICommandHandler<InspectJobOrderCommand, JobOrderDto>
{
    public async ValueTask<JobOrderDto> Handle(InspectJobOrderCommand command, CancellationToken cancellationToken)
    {
        var jo = await dbContext.JobOrders
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new AMIS.Framework.Core.Exceptions.NotFoundException($"Job order '{command.Id}' not found.");

        // The C.O./F.O. Inspector signatory is resolved from the authenticated identity, not the request body.
        var inspector = await SignatoryResolver.ResolveSignatoryAsync(currentUser, mediator, cancellationToken).ConfigureAwait(false);

        jo.Inspect(
            currentUser.GetUserId(),
            inspector.Name,
            inspector.Designation,
            command.InvoiceNo,
            command.InvoiceDate,
            command.DateInspected,
            command.Findings,
            command.FoundInOrder);

        jo.LastModifiedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreateJobOrderCommandHandler.MapToDto(jo);
    }
}
