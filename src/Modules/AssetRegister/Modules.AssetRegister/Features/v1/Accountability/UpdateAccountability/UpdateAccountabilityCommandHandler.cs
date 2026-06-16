using System.Net;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Accountability;
using AMIS.Modules.AssetRegister.Domain.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.UpdateAccountability;

public sealed class UpdateAccountabilityCommandHandler(AssetRegisterDbContext db, ICountFreezeGuard freezeGuard)
    : ICommandHandler<UpdateAccountabilityCommand, PropertyAccountabilityDto>
{
    public async ValueTask<PropertyAccountabilityDto> Handle(UpdateAccountabilityCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var accountability = await db.PropertyAccountabilities
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == cmd.AccountabilityId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Accountability '{cmd.AccountabilityId}' not found.");

        // ICS requires a caller-supplied expiry; PAR derives it server-side. Type is immutable here.
        if (accountability.AccountabilityType == AccountabilityType.SE_ICS && cmd.ExpiresOn is null)
            throw new CustomException("ICS accountability requires an expiry date.", Array.Empty<string>(), HttpStatusCode.BadRequest);

        // Header (also enforces the PendingAcceptance guard).
        var receivedBy = EmployeeRef.Create(cmd.ReceivedBy.EmployeeId, cmd.ReceivedBy.PrintedName, cmd.ReceivedBy.Designation);
        accountability.UpdateHeader(cmd.FundCluster, receivedBy, cmd.IssuedOn, cmd.ExpiresOn);

        // Reconcile the line set against the desired assets.
        var desiredById = cmd.Lines.ToDictionary(l => l.AssetRegistryId);
        var existingAssetIds = accountability.Lines.Select(l => l.AssetRegistryId).ToHashSet();
        var addedAssetIds = desiredById.Keys.Where(id => !existingAssetIds.Contains(id)).ToList();
        var removedLines = accountability.Lines.Where(l => !desiredById.ContainsKey(l.AssetRegistryId)).ToList();

        var touchedAssetIds = addedAssetIds.Concat(removedLines.Select(l => l.AssetRegistryId)).Distinct().ToList();
        var assets = await db.AssetRegistries
            .Where(a => touchedAssetIds.Contains(a.Id))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var assetById = assets.ToDictionary(a => a.Id);

        var missing = addedAssetIds.Except(assetById.Keys).ToList();
        if (missing.Count > 0)
            throw new NotFoundException($"Assets not found: {string.Join(", ", missing)}");

        await freezeGuard.EnsureMovementAllowedAsync(assets, cancellationToken).ConfigureAwait(false);

        // Add first, then remove — keeps the document from transiently dropping below one line
        // (the domain forbids removing the last line) during a full asset swap.
        foreach (var id in addedAssetIds)
        {
            var req = desiredById[id];
            var asset = assetById[id];
            VehicleAccountabilityProfile? vehicleProfile = null;
            if (req.OdometerAtIssue.HasValue || !string.IsNullOrWhiteSpace(req.PlateNumber)
                || !string.IsNullOrWhiteSpace(req.EngineNumber) || !string.IsNullOrWhiteSpace(req.ChassisNumber))
            {
                vehicleProfile = VehicleAccountabilityProfile.Create(
                    req.OdometerAtIssue, req.PlateNumber, req.EngineNumber, req.ChassisNumber);
            }
            accountability.AddLine(asset, req.ItemNo, req.ResponsibilityCenterCode, vehicleProfile);
            asset.AssignTo(accountability.Id, receivedBy.EmployeeId, req.LocationId);
        }

        foreach (var line in removedLines)
        {
            accountability.RemoveLine(line.Id);
            if (assetById.TryGetValue(line.AssetRegistryId, out var asset)
                && asset.LifecycleState == LifecycleState.Assigned)
            {
                asset.ReturnToAvailable();
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return AccountabilityMapper.ToDto(accountability);
    }
}
