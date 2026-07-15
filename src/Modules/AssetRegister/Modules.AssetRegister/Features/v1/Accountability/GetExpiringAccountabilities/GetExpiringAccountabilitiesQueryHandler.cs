using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Contracts.v1.SignedDocuments;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.GetExpiringAccountabilities;

public sealed class GetExpiringAccountabilitiesQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetExpiringAccountabilitiesQuery, IReadOnlyList<PropertyAccountabilitySummaryDto>>
{
    // Bounds the payload — the set of Active documents due for renewal within a couple of months is small,
    // but a cap keeps the dashboard call cheap even for an agency with a large fleet of PPE.
    private const int MaxResults = 200;

    public async ValueTask<IReadOnlyList<PropertyAccountabilitySummaryDto>> Handle(
        GetExpiringAccountabilitiesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var withinDays = query.WithinDays <= 0 ? 60 : query.WithinDays;
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(withinDays);

        // Active accountabilities whose expiry is on or before the cutoff (includes already-overdue ones,
        // which sort first). Renewed/Returned/Cancelled/PendingAcceptance never need a renewal reminder.
        var items = await db.PropertyAccountabilities.AsNoTracking()
            .Where(a => a.Status == AccountabilityStatus.Active
                        && a.ExpiresOn != null
                        && a.ExpiresOn <= cutoff)
            .OrderBy(a => a.ExpiresOn)
            .Take(MaxResults)
            .Select(a => new PropertyAccountabilitySummaryDto(
                a.Id, a.DocumentNo, a.AccountabilityType, a.Status, a.IssuedOn, a.ExpiresOn, a.Lines.Count,
                db.SignedDocuments.Any(sd => sd.DocumentType == AssetRegisterDocumentType.PropertyAccountability && sd.DocumentId == a.Id)))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return items;
    }
}
