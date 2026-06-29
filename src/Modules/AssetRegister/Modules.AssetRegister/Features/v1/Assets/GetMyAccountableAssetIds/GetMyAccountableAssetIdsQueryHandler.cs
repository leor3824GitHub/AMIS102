using AMIS.Framework.Core.Context;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Features.v1.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.GetMyAccountableAssetIds;

/// <summary>
/// Accepted-PAR gate: the assets a user may self-service are those carried on an <em>Active</em>
/// accountability (the officer has accepted the PAR) whose line is still Active and whose
/// <c>ReceivedBy</c> is the resolved current employee.
/// </summary>
public sealed class GetMyAccountableAssetIdsQueryHandler(
    ICurrentUser currentUser, IMediator mediator, AssetRegisterDbContext db)
    : IQueryHandler<GetMyAccountableAssetIdsQuery, IReadOnlySet<Guid>>
{
    public async ValueTask<IReadOnlySet<Guid>> Handle(GetMyAccountableAssetIdsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var employee = await CurrentEmployeeResolver.TryResolveAsync(currentUser, mediator, cancellationToken).ConfigureAwait(false);
        if (employee is null)
            return new HashSet<Guid>();

        var accountabilityType = query.AssetType == AssetType.SE
            ? AccountabilityType.SE_ICS
            : AccountabilityType.PPE_PAR;

        var ids = await db.PropertyAccountabilities
            .AsNoTracking()
            .Where(pa => pa.Status == AccountabilityStatus.Active
                && pa.AccountabilityType == accountabilityType
                && pa.ReceivedBy.EmployeeId == employee.Id)
            .SelectMany(pa => pa.Lines)
            .Where(l => l.LineStatus == AccountabilityLineStatus.Active)
            .Select(l => l.AssetRegistryId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return ids.ToHashSet();
    }
}
