using FSH.Framework.Shared.Persistence;
using FSH.Modules.AssetRegister.Contracts.v1.Accountability;
using FSH.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Features.v1.Accountability.SearchAccountabilities;

public sealed class SearchAccountabilitiesQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<SearchAccountabilitiesQuery, PagedResponse<PropertyAccountabilitySummaryDto>>
{
    public async ValueTask<PagedResponse<PropertyAccountabilitySummaryDto>> Handle(
        SearchAccountabilitiesQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = db.PropertyAccountabilities.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.ToLowerInvariant();
            q = q.Where(a => a.DocumentNo.ToLower().Contains(k));
        }
        if (query.Type.HasValue) q = q.Where(a => a.AccountabilityType == query.Type.Value);
        if (query.Status.HasValue) q = q.Where(a => a.Status == query.Status.Value);
        if (query.ReceivedByEmployeeId.HasValue) q = q.Where(a => a.ReceivedBy.EmployeeId == query.ReceivedByEmployeeId.Value);
        if (query.FromDate.HasValue) q = q.Where(a => a.IssuedOn >= query.FromDate.Value);
        if (query.ToDate.HasValue) q = q.Where(a => a.IssuedOn <= query.ToDate.Value);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var total = await q.LongCountAsync(ct).ConfigureAwait(false);
        var items = await q.OrderByDescending(a => a.IssuedOn)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(a => new PropertyAccountabilitySummaryDto(
                a.Id, a.DocumentNo, a.AccountabilityType, a.Status, a.IssuedOn, a.ExpiresOn, a.Lines.Count))
            .ToListAsync(ct).ConfigureAwait(false);

        return new PagedResponse<PropertyAccountabilitySummaryDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }
}
