using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Accountability;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Services;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.Notifications.Contracts.Events;
using AMIS.Modules.Notifications.Contracts.v1.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.IssueAccountability;

public sealed class IssueAccountabilityCommandHandler(
    AssetRegisterDbContext db,
    IAccountabilityNumberGenerator numberGenerator,
    ICountFreezeGuard freezeGuard,
    IMediator mediator,
    IEventBus eventBus,
    ILogger<IssueAccountabilityCommandHandler> logger)
    : ICommandHandler<IssueAccountabilityCommand, PropertyAccountabilityDto>
{
    public async ValueTask<PropertyAccountabilityDto> Handle(IssueAccountabilityCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var assetIds = cmd.Lines.Select(l => l.AssetRegistryId).ToList();
        var assets = await db.AssetRegistries
            .Where(a => assetIds.Contains(a.Id))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var missing = assetIds.Except(assets.Select(a => a.Id)).ToList();
        if (missing.Count > 0)
            throw new KeyNotFoundException($"Assets not found: {string.Join(", ", missing)}");

        await freezeGuard.EnsureMovementAllowedAsync(assets, cancellationToken).ConfigureAwait(false);

        var assetById = assets.ToDictionary(a => a.Id);

        var documentNo = cmd.AccountabilityType == AccountabilityType.SE_ICS
            ? await numberGenerator.NextIcsAsync(InferIcsCategory(assets), cmd.IssuedOn, cancellationToken).ConfigureAwait(false)
            : await numberGenerator.NextParAsync(cmd.IssuedOn, cancellationToken).ConfigureAwait(false);

        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var issuedBy = EmployeeRef.Create(cmd.IssuedBy.EmployeeId, cmd.IssuedBy.PrintedName, cmd.IssuedBy.Designation);
        var receivedBy = EmployeeRef.Create(cmd.ReceivedBy.EmployeeId, cmd.ReceivedBy.PrintedName, cmd.ReceivedBy.Designation);

        var domainLines = cmd.Lines.Select(l =>
        {
            var asset = assetById[l.AssetRegistryId];
            VehicleAccountabilityProfile? vehicleProfile = null;
            if (l.OdometerAtIssue.HasValue || !string.IsNullOrWhiteSpace(l.PlateNumber)
                || !string.IsNullOrWhiteSpace(l.EngineNumber) || !string.IsNullOrWhiteSpace(l.ChassisNumber))
            {
                vehicleProfile = VehicleAccountabilityProfile.Create(
                    l.OdometerAtIssue, l.PlateNumber, l.EngineNumber, l.ChassisNumber);
            }
            return (asset, l.ItemNo, l.ResponsibilityCenterCode, vehicleProfile);
        });

        var accountability = PropertyAccountability.Issue(
            tenantId, cmd.AccountabilityType, documentNo, cmd.FundCluster,
            issuedBy, receivedBy, cmd.IssuedOn, cmd.ExpiresOn, domainLines);

        var locationByAsset = cmd.Lines.ToDictionary(l => l.AssetRegistryId, l => l.LocationId);

        // Mutate each asset to Assigned (lifecycle moves alongside the issuance).
        foreach (var line in accountability.Lines)
        {
            var asset = assetById[line.AssetRegistryId];
            asset.AssignTo(accountability.Id, receivedBy.EmployeeId, locationByAsset[line.AssetRegistryId]);
        }

        db.PropertyAccountabilities.Add(accountability);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // The ICS/PAR is now PendingAcceptance — notify the accountable person so they can review and accept it.
        await NotifyReceivingEmployeeAsync(accountability, cancellationToken).ConfigureAwait(false);

        return AccountabilityMapper.ToDto(accountability);
    }

    /// <summary>
    /// Best-effort: resolve the receiving employee's login account and drop an "awaiting acceptance" notification
    /// in their bell. Runs after commit and never throws — a notification failure must not fail the issuance.
    /// A receiver with no linked login (<c>IdentityUserId</c> null) has no inbox to target, so it is skipped.
    /// </summary>
    private async Task NotifyReceivingEmployeeAsync(PropertyAccountability accountability, CancellationToken cancellationToken)
    {
        var docLabel = accountability.AccountabilityType == AccountabilityType.SE_ICS ? "ICS" : "PAR";
        try
        {
            var receiver = await mediator
                .Send(new GetEmployeeReferenceByIdQuery(accountability.ReceivedBy.EmployeeId), cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(receiver?.IdentityUserId))
            {
                logger.LogWarning(
                    "{DocLabel} {DocumentNo}: receiving employee {EmployeeId} has no linked login account; acceptance notification skipped.",
                    docLabel, accountability.DocumentNo, accountability.ReceivedBy.EmployeeId);
                return;
            }

            var notification = new NotificationRequestedIntegrationEvent(
                RecipientUserId: receiver.IdentityUserId,
                Type: NotificationType.AccountabilityIssued,
                Title: $"{docLabel} issued for your acceptance",
                Body: $"{docLabel} {accountability.DocumentNo} has been issued to you. Please review and accept it.",
                Link: "/asset-register/my-accountability",
                Source: "AssetRegister",
                MetadataJson: null,
                TenantId: accountability.TenantId,
                CorrelationId: accountability.Id.ToString());

            await eventBus.PublishAsync(notification, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish acceptance notification for {DocLabel} {DocumentId}.", docLabel, accountability.Id);
        }
    }

    private static AssetCategory InferIcsCategory(IEnumerable<AssetRegistry> assets)
    {
        // ICS prefix: SPLV (low-valued) or SPHV (high-valued). If any asset on the doc
        // is high-valued, the whole doc gets the SPHV prefix.
        return assets.Any(a => a.Category == AssetCategory.HighValuedSemi)
            ? AssetCategory.HighValuedSemi
            : AssetCategory.LowValuedSemi;
    }
}

