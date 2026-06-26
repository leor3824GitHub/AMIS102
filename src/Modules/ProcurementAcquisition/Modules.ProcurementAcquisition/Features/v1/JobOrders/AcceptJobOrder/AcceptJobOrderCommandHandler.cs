using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.CreateJobOrder;
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
            ?? throw new NotFoundException($"Job order '{command.Id}' not found.");

        // The Supply Officer signatory is the Organization Profile's Property/Supply Custodian. Only that
        // person may accept; the printed name comes from the org profile, so it must be configured first.
        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);
        if (org?.PropertyCustodianId is not { } supplyOfficerId || supplyOfficerId == Guid.Empty)
            throw new CustomException(
                "No Supply Officer is configured. Set the Property/Supply Custodian in the Organization Profile before accepting job orders.",
                [], System.Net.HttpStatusCode.BadRequest);

        var identityUserId = currentUser.GetUserId().ToString();
        var employee = await mediator.Send(new GetEmployeeReferenceByIdentityUserIdQuery(identityUserId), cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("No employee profile found for the current user. Cannot record acceptance.");

        if (employee.Id != supplyOfficerId)
            throw new ForbiddenException("Only the designated Supply Officer (Property/Supply Custodian) can accept job orders.");

        try
        {
            jo.Accept(
                employee.Id,
                command.InvoiceNo,
                command.DateReceived,
                command.IsCompleteDelivery,
                command.PartialDeliveryNote);
        }
        catch (InvalidOperationException ex)
        {
            throw new CustomException(ex.Message, [], System.Net.HttpStatusCode.BadRequest);
        }

        jo.LastModifiedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreateJobOrderCommandHandler.MapToDto(jo);
    }
}
