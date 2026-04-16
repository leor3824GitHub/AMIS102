using FSH.Modules.MasterData.Contracts.v1.PropertyClasses;
using FSH.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.PropertyClasses.GetPropertyClassTree;

public sealed class GetPropertyClassTreeQueryHandler(MasterDataDbContext db)
    : IQueryHandler<GetPropertyClassTreeQuery, IReadOnlyList<PropertyClassDto>>
{
    public async ValueTask<IReadOnlyList<PropertyClassDto>> Handle(
        GetPropertyClassTreeQuery query, CancellationToken cancellationToken)
    {
        return await db.PropertyClasses
            .Include(x => x.Items)
            .OrderBy(x => x.Code)
            .Select(x => new PropertyClassDto(
                x.Id,
                x.Code,
                x.Name,
                x.Description,
                x.IsActive,
                x.Items
                    .OrderBy(i => i.ItemCode)
                    .Select(i => new PropertyClassItemDto(
                        i.Id,
                        i.ClassCode,
                        i.ItemCode,
                        i.Name,
                        i.Description,
                        i.IsActive))
                    .ToList()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
