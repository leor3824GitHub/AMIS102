using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Contracts.v1.Repairs;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Repairs;
using AMIS.Modules.Identity.Contracts.Services;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.Notifications.Contracts.Events;
using AMIS.Modules.Notifications.Contracts.v1.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.AssetRegister.Features.v1.Repairs;

public sealed class RequestRepairCommandHandler(
    AssetRegisterDbContext db,
    ICurrentUser currentUser,
    IMediator mediator,
    IUserService userService,
    IEventBus eventBus,
    ILogger<RequestRepairCommandHandler> logger)
    : ICommandHandler<RequestRepairCommand, PropertyRepairDto>
{
    public async ValueTask<PropertyRepairDto> Handle(RequestRepairCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var tenantId = currentUser.GetTenant() ?? throw new InvalidOperationException("Tenant ID required");

        var asset = await db.AssetRegistries.FirstOrDefaultAsync(a => a.Id == cmd.AssetRegistryId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Asset not found.");

        // Auto-fill Nature/Date of Last Repair from the latest accepted RPRI of this asset.
        var lastAccepted = await db.PropertyRepairs.AsNoTracking()
            .Where(x => x.AssetRegistryId == cmd.AssetRegistryId && x.Status == RepairStatus.Accepted)
            .OrderByDescending(x => x.AcceptedOn)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var repair = PropertyRepair.Request(
            tenantId, cmd.AssetRegistryId, GenerateRpriNo(),
            cmd.NatureOfWork, cmd.PartsToReplace, cmd.RequestedBy, cmd.RequestedOn,
            cmd.EngineNo, cmd.ChassisNo, cmd.OdometerReading,
            lastAccepted?.NatureOfWork, lastAccepted?.AcceptedOn,
            cmd.InspectorId, cmd.InspectorName, cmd.NotedBy);
        repair.SetCreatedBy(currentUser.GetUserId().ToString());

        db.PropertyRepairs.Add(repair);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // The RPRI is now Requested and awaiting mandatory pre-repair inspection — notify the inspector.
        await NotifyInspectorAsync(repair, asset.PropertyNo.Value, tenantId, cancellationToken).ConfigureAwait(false);

        return repair.ToDto(asset);
    }

    /// <summary>
    /// Best-effort: drop a "pre-repair inspection requested" notification in the inspector's bell. When a
    /// specific inspector was assigned on the request, only that person is notified; otherwise it fans out
    /// to every user who can inspect (holds <see cref="AssetRegisterPermissions.Repair.Inspect"/>). Runs
    /// after commit and never throws — a notification failure must not fail the RPRI request. The requester
    /// is skipped, and a recipient with no linked login has no inbox to target and is skipped.
    /// </summary>
    private async Task NotifyInspectorAsync(PropertyRepair repair, string propertyNo, string tenantId, CancellationToken ct)
    {
        try
        {
            var recipientUserIds = await ResolveRecipientUserIdsAsync(repair, ct).ConfigureAwait(false);

            foreach (var userId in recipientUserIds)
            {
                var notification = new NotificationRequestedIntegrationEvent(
                    RecipientUserId: userId,
                    Type: NotificationType.InspectionRequested,
                    Title: "Pre-repair inspection requested",
                    Body: $"RPRI {repair.RpriNo} for property {propertyNo} is awaiting pre-repair inspection.",
                    Link: "/asset-register/repairs",
                    Source: "AssetRegister",
                    MetadataJson: null,
                    TenantId: tenantId,
                    CorrelationId: repair.Id.ToString());

                await eventBus.PublishAsync(notification, ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish pre-repair inspection notification for RPRI {RpriId}.", repair.Id);
        }
    }

    private async Task<IReadOnlyList<string>> ResolveRecipientUserIdsAsync(PropertyRepair repair, CancellationToken ct)
    {
        // A specific inspector was assigned on the request — notify just them.
        if (repair.InspectorId is { } inspectorId && inspectorId != Guid.Empty)
        {
            var inspector = await mediator.Send(new GetEmployeeReferenceByIdQuery(inspectorId), ct).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(inspector?.IdentityUserId))
                return [inspector!.IdentityUserId!];

            logger.LogWarning(
                "RPRI {RpriNo}: assigned inspector {InspectorId} has no linked login account; notification skipped.",
                repair.RpriNo, inspectorId);
            return [];
        }

        // No specific inspector — fall back to every user who can perform pre-repair inspection.
        var requesterId = currentUser.GetUserId().ToString();
        var users = await userService.GetListAsync(ct).ConfigureAwait(false);
        var recipients = new List<string>();
        foreach (var user in users)
        {
            if (string.IsNullOrWhiteSpace(user.Id) || string.Equals(user.Id, requesterId, StringComparison.Ordinal))
                continue;
            if (await userService.HasPermissionAsync(user.Id, AssetRegisterPermissions.Repair.Inspect, ct).ConfigureAwait(false))
                recipients.Add(user.Id);
        }
        return recipients;
    }

    private static string GenerateRpriNo() =>
        $"RPRI-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}
