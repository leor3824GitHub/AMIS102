using AMIS.Framework.Core.Context;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.CreateJobOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.CertifyFundsAvailable;

public sealed class CertifyJobOrderFundsAvailableCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator) : ICommandHandler<CertifyJobOrderFundsAvailableCommand, JobOrderDto>
{
    public async ValueTask<JobOrderDto> Handle(CertifyJobOrderFundsAvailableCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var jo = await dbContext.JobOrders
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new AMIS.Framework.Core.Exceptions.NotFoundException($"Job order '{command.Id}' not found.");

        var accountantId = currentUser.GetUserId();
        // Signatory name + designation come from the authenticated identity, never from the request body.
        var certifier = await SignatoryResolver.ResolveSignatoryAsync(currentUser, mediator, cancellationToken).ConfigureAwait(false);

        jo.CertifyFundsAvailable(
            accountantId,
            certifier.Name,
            command.OursBursNumber,
            command.OursBursDate,
            command.FundCluster,
            certifiedByDesignation: certifier.Designation);

        jo.LastModifiedBy = accountantId.ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return CreateJobOrderCommandHandler.MapToDto(jo);
    }
}
