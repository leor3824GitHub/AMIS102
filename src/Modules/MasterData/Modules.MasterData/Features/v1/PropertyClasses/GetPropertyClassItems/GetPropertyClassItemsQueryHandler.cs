using FSH.Modules.MasterData.Contracts.v1.PropertyClasses;
using FSH.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.PropertyClasses.GetPropertyClassItems;

public sealed class GetPropertyClassItemsQueryHandler(MasterDataDbContext db)
    : IQueryHandler<GetPropertyClassItemsQuery, IReadOnlyList<PropertyClassItemDto>>
{
    public async ValueTask<IReadOnlyList<PropertyClassItemDto>> Handle(
        GetPropertyClassItemsQuery query, CancellationToken cancellationToken)
    {
        var q = db.PropertyClassItems.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(query.ClassCode))
            q = q.Where(x => x.ClassCode == query.ClassCode.ToUpperInvariant());

        return await q
            .OrderBy(x => x.ClassCode)
            .ThenBy(x => x.ItemCode)
            .Select(x => new PropertyClassItemDto(
                x.Id,
                x.ClassCode,
                x.ItemCode,
                x.Name,
                x.Description,
                x.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
