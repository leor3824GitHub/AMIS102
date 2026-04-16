using FSH.Modules.MasterData.Contracts.v1.PropertyClasses;
using FSH.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.PropertyClasses.GetPropertyClassById;

public sealed class GetPropertyClassByIdQueryHandler(MasterDataDbContext db)
    : IQueryHandler<GetPropertyClassByIdQuery, PropertyClassDto?>
{
    public async ValueTask<PropertyClassDto?> Handle(
        GetPropertyClassByIdQuery query, CancellationToken cancellationToken)
    {
        return await db.PropertyClasses
            .Include(x => x.Items)
            .Where(x => x.Id == query.Id)
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
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
