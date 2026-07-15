using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Contracts.v1.SignedDocuments;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.SearchAccountabilities;

public sealed class SearchAccountabilitiesQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<SearchAccountabilitiesQuery, PagedResponse<PropertyAccountabilitySummaryDto>>
{
    public async ValueTask<PagedResponse<PropertyAccountabilitySummaryDto>> Handle(
        SearchAccountabilitiesQuery query, CancellationToken cancellationToken)
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

        var total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var items = await q.OrderByDescending(a => a.IssuedOn)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(a => new PropertyAccountabilitySummaryDto(
                a.Id, a.DocumentNo, a.AccountabilityType, a.Status, a.IssuedOn, a.ExpiresOn, a.Lines.Count,
                db.SignedDocuments.Any(sd => sd.DocumentType == AssetRegisterDocumentType.PropertyAccountability && sd.DocumentId == a.Id),
                // Latest outstanding (Pending/Inspected) return request against this document — surfaces the
                // in-flight return on "My Accountability" so the requester can track/withdraw it there. Both
                // subqueries share the same ordering so the id and status come from the same receipt.
                db.ReturnedPropertyReceipts
                    .Where(r => r.AccountabilityId == a.Id
                        && (r.Status == ReturnedPropertyReceiptStatus.Pending || r.Status == ReturnedPropertyReceiptStatus.Inspected))
                    .OrderByDescending(r => r.Date).ThenByDescending(r => r.CreatedOnUtc)
                    .Select(r => (Guid?)r.Id).FirstOrDefault(),
                db.ReturnedPropertyReceipts
                    .Where(r => r.AccountabilityId == a.Id
                        && (r.Status == ReturnedPropertyReceiptStatus.Pending || r.Status == ReturnedPropertyReceiptStatus.Inspected))
                    .OrderByDescending(r => r.Date).ThenByDescending(r => r.CreatedOnUtc)
                    .Select(r => (ReturnedPropertyReceiptStatus?)r.Status).FirstOrDefault()))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

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

