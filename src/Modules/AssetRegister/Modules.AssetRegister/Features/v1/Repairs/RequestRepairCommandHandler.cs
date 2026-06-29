using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1.Repairs;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Repairs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Repairs;

public sealed class RequestRepairCommandHandler(AssetRegisterDbContext db, ICurrentUser currentUser)
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
            lastAccepted?.NatureOfWork, lastAccepted?.AcceptedOn);
        repair.SetCreatedBy(currentUser.GetUserId().ToString());

        db.PropertyRepairs.Add(repair);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return repair.ToDto(asset);
    }

    private static string GenerateRpriNo() =>
        $"RPRI-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}
