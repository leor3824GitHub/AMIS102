using AMIS.Framework.Core.Context;
using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.Notifications.Contracts.Events;
using AMIS.Modules.Notifications.Contracts.v1.Enums;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.CreateJobOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.IssueJobOrder;

public sealed class IssueJobOrderCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator,
    IEventBus eventBus,
    ILogger<IssueJobOrderCommandHandler> logger) : ICommandHandler<IssueJobOrderCommand, JobOrderDto>
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

        // The JO is now Issued and inspectable — notify the assigned inspector that an inspection is requested.
        await NotifyInspectorAsync(jo, cancellationToken).ConfigureAwait(false);

        return CreateJobOrderCommandHandler.MapToDto(jo);
    }

    /// <summary>
    /// Best-effort: resolve the assigned inspector's login account and drop an inspection-request notification
    /// in their bell. Runs after commit and never throws — a notification failure must not fail the Issue.
    /// An inspector with no linked login (<c>IdentityUserId</c> null) has no inbox to target, so it is skipped.
    /// </summary>
    private async Task NotifyInspectorAsync(Domain.JobOrders.JobOrder jo, CancellationToken cancellationToken)
    {
        try
        {
            var inspector = await mediator
                .Send(new GetEmployeeReferenceByIdQuery(jo.InspectorId), cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(inspector?.IdentityUserId))
            {
                logger.LogWarning(
                    "JO {JoNumber}: inspector {InspectorId} has no linked login account; inspection-request notification skipped.",
                    jo.JoNumber, jo.InspectorId);
                return;
            }

            var notification = new NotificationRequestedIntegrationEvent(
                RecipientUserId: inspector.IdentityUserId,
                Type: NotificationType.InspectionRequested,
                Title: "Inspection requested",
                Body: $"Job Order {jo.JoNumber} is issued and ready for your inspection.",
                Link: $"/procurement/job-orders?inspect={jo.Id}",
                Source: "ProcurementAcquisition",
                MetadataJson: null,
                TenantId: jo.TenantId,
                CorrelationId: jo.Id.ToString());

            await eventBus.PublishAsync(notification, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish inspection-request notification for JO {JoId}.", jo.Id);
        }
    }
}
