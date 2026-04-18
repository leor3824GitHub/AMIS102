using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.TangibleItems.GetNextTangibleItemSequence;

public sealed record GetNextTangibleItemSequenceQuery(
    int Year,
    string OfficeCode,
    string ClassCode,
    string ItemCode) : IQuery<NextTangibleItemSequenceResponse>;

public sealed record NextTangibleItemSequenceResponse(int NextSequence);

public sealed class GetNextTangibleItemSequenceQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetNextTangibleItemSequenceQuery, NextTangibleItemSequenceResponse>
{
    public async ValueTask<NextTangibleItemSequenceResponse> Handle(
        GetNextTangibleItemSequenceQuery query, CancellationToken cancellationToken)
    {
        var prefix = $"{query.Year}-NFA-{query.OfficeCode}-{query.ClassCode}-{query.ItemCode}-";

        var maxSeq = await dbContext.TangibleItems
            .Where(x => x.PropertyClass == query.ClassCode
                     && x.CategoryCode == query.ItemCode
                     && x.PropertyNo.StartsWith(prefix))
            .Select(x => x.PropertyNo.Substring(prefix.Length))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var next = maxSeq
            .Select(s => int.TryParse(s, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        return new NextTangibleItemSequenceResponse(next);
    }
}
