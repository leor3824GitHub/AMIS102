using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.GetNextPropertyNoSequence;

public sealed class GetNextPropertyNoSequenceQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetNextPropertyNoSequenceQuery, NextPropertyNoSequenceResponse>
{
    public async ValueTask<NextPropertyNoSequenceResponse> Handle(
        GetNextPropertyNoSequenceQuery query, CancellationToken cancellationToken)
    {
        // Matches the retired AssetManagement generator: property numbers are formatted
        // "{Year}-NFA-{OfficeCode}-{ClassCode}-{sequence}"; the next sequence is the max
        // trailing number for that prefix + 1. PropertyNo is a value-object → string column,
        // so scan the underlying column via EF.Property to keep the filter server-side.
        var prefix = $"{query.Year}-NFA-{query.OfficeCode}-{query.ClassCode}-";

        var existing = await db.AssetRegistries
            .Where(a => a.PropertyClass == query.ClassCode
                     && EF.Property<string>(a, "PropertyNo").StartsWith(prefix))
            .Select(a => EF.Property<string>(a, "PropertyNo"))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var next = existing
            .Select(v => int.TryParse(v[prefix.Length..], out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        return new NextPropertyNoSequenceResponse(next);
    }
}
