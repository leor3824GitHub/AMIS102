using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.GetPhysicalCountSession;

public sealed class GetPhysicalCountSessionQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetPhysicalCountSessionQuery, PhysicalCountSessionDto?>
{
    public async ValueTask<PhysicalCountSessionDto?> Handle(GetPhysicalCountSessionQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var session = await db.PhysicalCountSessions
            .AsNoTracking()
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == query.Id, ct).ConfigureAwait(false);
        return session is null ? null : CountingMapper.ToDto(session);
    }
}

