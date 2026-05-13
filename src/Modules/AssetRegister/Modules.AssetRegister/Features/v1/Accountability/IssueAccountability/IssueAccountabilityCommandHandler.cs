using FSH.Modules.AssetRegister.Contracts.v1;
using FSH.Modules.AssetRegister.Contracts.v1.Accountability;
using FSH.Modules.AssetRegister.Contracts.v1.ValueObjects;
using FSH.Modules.AssetRegister.Data;
using FSH.Modules.AssetRegister.Domain.Accountability;
using FSH.Modules.AssetRegister.Domain.Assets;
using FSH.Modules.AssetRegister.Domain.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Features.v1.Accountability.IssueAccountability;

public sealed class IssueAccountabilityCommandHandler(
    AssetRegisterDbContext db,
    IAccountabilityNumberGenerator numberGenerator)
    : ICommandHandler<IssueAccountabilityCommand, PropertyAccountabilityDto>
{
    public async ValueTask<PropertyAccountabilityDto> Handle(IssueAccountabilityCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var assetIds = cmd.Lines.Select(l => l.AssetRegistryId).ToList();
        var assets = await db.AssetRegistries
            .Where(a => assetIds.Contains(a.Id))
            .ToListAsync(ct).ConfigureAwait(false);

        var missing = assetIds.Except(assets.Select(a => a.Id)).ToList();
        if (missing.Count > 0)
            throw new KeyNotFoundException($"Assets not found: {string.Join(", ", missing)}");

        var assetById = assets.ToDictionary(a => a.Id);

        var documentNo = cmd.AccountabilityType == AccountabilityType.SE_ICS
            ? await numberGenerator.NextIcsAsync(InferIcsCategory(assets), cmd.IssuedOn, ct).ConfigureAwait(false)
            : await numberGenerator.NextParAsync(cmd.IssuedOn, ct).ConfigureAwait(false);

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
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return AccountabilityMapper.ToDto(accountability);
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
