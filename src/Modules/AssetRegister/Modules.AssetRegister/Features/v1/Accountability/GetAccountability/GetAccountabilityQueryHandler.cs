using FSH.Modules.AssetRegister.Contracts.v1.Accountability;
using FSH.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Features.v1.Accountability.GetAccountability;

public sealed class GetAccountabilityQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetAccountabilityQuery, PropertyAccountabilityDto?>
{
    public async ValueTask<PropertyAccountabilityDto?> Handle(GetAccountabilityQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var entity = await db.PropertyAccountabilities
            .AsNoTracking()
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == query.Id, ct).ConfigureAwait(false);
        return entity is null ? null : AccountabilityMapper.ToDto(entity);
    }
}
