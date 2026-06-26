using AMIS.Framework.Core.Context;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.CreateJobOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.IssueJobOrder;

public sealed class IssueJobOrderCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator) : ICommandHandler<IssueJobOrderCommand, JobOrderDto>
{
    public async ValueTask<JobOrderDto> Handle(IssueJobOrderCommand command, CancellationToken cancellationToken)
    {
        var jo = await dbContext.JobOrders
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new AMIS.Framework.Core.Exceptions.NotFoundException($"Job order '{command.Id}' not found.");

        // Signatory name + designation come from the authenticated identity (employee profile), not a request value.
        var issuer = await SignatoryResolver.ResolveSignatoryAsync(currentUser, mediator, cancellationToken).ConfigureAwait(false);
        jo.Issue(currentUser.GetUserId(), issuer.Name, issuer.Designation);
        jo.LastModifiedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreateJobOrderCommandHandler.MapToDto(jo);
    }
}
