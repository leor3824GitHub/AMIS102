using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Transfers.SearchTransferOffers;

/// <summary>
/// Lists this agency's transfer offers — inbound (the "Incoming Transfers" inbox) or outbound (what we
/// offered other agencies). No cross-tenant read happens here: the ambient tenant filter scopes the query
/// to this tenant's own rows, exactly as with any other entity.
/// </summary>
public sealed class SearchTransferOffersQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<SearchTransferOffersQuery, PagedResponse<AssetTransferOfferSummaryDto>>
{
    public async ValueTask<PagedResponse<AssetTransferOfferSummaryDto>> Handle(
        SearchTransferOffersQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = db.AssetTransferOffers.AsNoTracking().AsQueryable();

        if (query.Direction.HasValue) q = q.Where(o => o.Direction == query.Direction.Value);
        if (query.Status.HasValue) q = q.Where(o => o.Status == query.Status.Value);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.ToLowerInvariant();
            q = q.Where(o => o.SourceIssuanceReportNo.ToLower().Contains(k)
                          || o.FromAgencyName.ToLower().Contains(k)
                          || o.ToAgencyName.ToLower().Contains(k));
        }

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var items = await q.OrderByDescending(o => o.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(o => new AssetTransferOfferSummaryDto(
                o.Id, o.CorrelationId, o.Direction, o.FromAgencyName, o.ToAgencyName,
                o.SourceIssuanceReportNo, o.IssuanceReportType, o.Status, o.ReceivingReportNo,
                o.CreatedOnUtc, o.RespondedUtc,
                o.Lines.Count,
                o.Lines.Sum(l => l.NetBookValue)))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new PagedResponse<AssetTransferOfferSummaryDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }
}
