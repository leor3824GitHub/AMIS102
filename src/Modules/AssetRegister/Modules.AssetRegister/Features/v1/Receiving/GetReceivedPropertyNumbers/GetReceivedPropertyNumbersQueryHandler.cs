using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.GetReceivedPropertyNumbers;

/// <summary>
/// Resolves which of the candidate property numbers have already been received on an SMRR/PPERR line.
/// The join key is <c>ReceivingReportItem.PropertyNo</c> == the IAR line's <c>StockPropertyNo</c>.
/// </summary>
public sealed class GetReceivedPropertyNumbersQueryHandler(
    AssetRegisterDbContext db) : IQueryHandler<GetReceivedPropertyNumbersQuery, IReadOnlyCollection<string>>
{
    public async ValueTask<IReadOnlyCollection<string>> Handle(
        GetReceivedPropertyNumbersQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var candidates = query.PropertyNumbers?
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        if (candidates.Count == 0)
            return [];

        return await db.ReceivingReports
            .AsNoTracking()
            .SelectMany(r => r.Items)
            .Where(i => candidates.Contains(i.PropertyNo))
            .Select(i => i.PropertyNo)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
