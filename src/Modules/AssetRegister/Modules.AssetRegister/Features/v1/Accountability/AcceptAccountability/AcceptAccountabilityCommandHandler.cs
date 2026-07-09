using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Accountability;
using AMIS.Modules.AssetRegister.Features.v1.Shared;
using AMIS.Modules.Notifications.Contracts.Events;
using AMIS.Modules.Notifications.Contracts.v1.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.AcceptAccountability;

public sealed class AcceptAccountabilityCommandHandler(
    AssetRegisterDbContext db,
    ICurrentUser currentUser,
    IMediator mediator,
    IEventBus eventBus,
    ILogger<AcceptAccountabilityCommandHandler> logger)
    : ICommandHandler<AcceptAccountabilityCommand, PropertyAccountabilityDto>
{
    public async ValueTask<PropertyAccountabilityDto> Handle(AcceptAccountabilityCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var accountability = await db.PropertyAccountabilities
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == cmd.AccountabilityId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Accountability '{cmd.AccountabilityId}' not found.");

        // Ownership gate: only the named recipient may accept — even with the Acknowledge permission.
        var employee = await CurrentEmployeeResolver.TryResolveAsync(currentUser, mediator, cancellationToken).ConfigureAwait(false)
            ?? throw new ForbiddenException("No employee profile is linked to your account.");
        if (accountability.ReceivedBy.EmployeeId != employee.Id)
            throw new ForbiddenException("You can only accept accountabilities issued to you.");

        // Cross-client race: the same person may accept this document from the MAUI app and the
        // web UI at the same time. Whoever loses the race gets a clear 409 instead of a generic 500,
        // so the UI can tell them it was already accepted (and refresh) rather than swallowing it.
        if (accountability.Status != AccountabilityStatus.PendingAcceptance)
        {
            var label = accountability.AccountabilityType == AccountabilityType.SE_ICS ? "ICS" : "PAR";
            var message = accountability.Status == AccountabilityStatus.Active && accountability.AcceptedOn is { } acceptedOn
                ? $"{label} '{accountability.DocumentNo}' was already accepted on {acceptedOn:yyyy-MM-dd}. Refresh to see its current status."
                : $"{label} '{accountability.DocumentNo}' can no longer be accepted because it is {accountability.Status}. Refresh to see its current status.";
            throw new CustomException(message, (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        accountability.Accept(cmd.AcceptedOn);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // The "issued for your acceptance" bell entry is now actioned — resolve it to read so the
        // recipient isn't nagged about a document they just accepted.
        await MarkIssuanceNotificationReadAsync(accountability, cancellationToken).ConfigureAwait(false);

        return AccountabilityMapper.ToDto(accountability);
    }

    /// <summary>
    /// Best-effort: mark the recipient's <see cref="NotificationType.AccountabilityIssued"/> notification
    /// read (keyed by the same CorrelationId the issuance published). The ownership gate above guarantees
    /// the caller IS the notified recipient. Never throws — a bell hiccup must not fail the acceptance.
    /// </summary>
    private async Task MarkIssuanceNotificationReadAsync(PropertyAccountability accountability, CancellationToken cancellationToken)
    {
        try
        {
            var readRequest = new NotificationReadRequestedIntegrationEvent(
                RecipientUserId: currentUser.GetUserId().ToString(),
                Type: NotificationType.AccountabilityIssued,
                Source: "AssetRegister",
                CorrelationId: accountability.Id.ToString(),
                TenantId: accountability.TenantId);

            await eventBus.PublishAsync(readRequest, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to mark issuance notification read for accountability {Id}.", accountability.Id);
        }
    }
}
