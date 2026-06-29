using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.GetAccountableOfficersByAssetIds;

/// <summary>
/// Resolves the current accountable officer (Active PAR's <c>ReceivedBy</c>) for each requested asset,
/// in a single round-trip. Mirrors the current-accountability resolution used by <c>AssetScanDetailDto</c>.
/// </summary>
public sealed class GetAccountableOfficersByAssetIdsQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetAccountableOfficersByAssetIdsQuery, IReadOnlyDictionary<Guid, AccountableOfficerDto>>
{
    public async ValueTask<IReadOnlyDictionary<Guid, AccountableOfficerDto>> Handle(
        GetAccountableOfficersByAssetIdsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var ids = query.AssetRegistryIds as IList<Guid> ?? query.AssetRegistryIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, AccountableOfficerDto>();

        var rows = await db.PropertyAccountabilities
            .AsNoTracking()
            .Where(pa => pa.Status == AccountabilityStatus.Active)
            .SelectMany(pa => pa.Lines
                .Where(l => l.LineStatus == AccountabilityLineStatus.Active && ids.Contains(l.AssetRegistryId))
                .Select(l => new
                {
                    l.AssetRegistryId,
                    pa.ReceivedBy.EmployeeId,
                    pa.ReceivedBy.PrintedName,
                    pa.ReceivedBy.Designation
                }))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .GroupBy(r => r.AssetRegistryId)
            .ToDictionary(
                g => g.Key,
                g => new AccountableOfficerDto(g.First().EmployeeId, g.First().PrintedName, g.First().Designation));
    }
}
