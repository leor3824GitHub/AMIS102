using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.ThresholdPolicies.GetThresholdPolicyHistory;

public sealed class GetThresholdPolicyHistoryQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetThresholdPolicyHistoryQuery, PagedThresholdPolicyHistoryResponse>
{
    public async ValueTask<PagedThresholdPolicyHistoryResponse> Handle(GetThresholdPolicyHistoryQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 20 : query.PageSize;

        var q = dbContext.CapitalizationThresholdPolicies.AsQueryable();

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await q
            .OrderByDescending(x => x.EffectiveDate)
            .ThenByDescending(x => x.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ThresholdPolicyHistoryDto(
                x.Id,
                x.LowValueThreshold,
                x.CapitalizationThreshold,
                x.EffectiveDate,
                x.IsActive,
                x.Reason,
                x.CreatedOnUtc,
                x.CreatedBy))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedThresholdPolicyHistoryResponse(items, pageNumber, pageSize, totalCount);
    }
}
