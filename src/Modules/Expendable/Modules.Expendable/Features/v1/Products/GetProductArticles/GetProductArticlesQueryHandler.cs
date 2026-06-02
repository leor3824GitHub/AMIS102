using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Products.GetProductArticles;

public sealed class GetProductArticlesQueryHandler : IQueryHandler<GetProductArticlesQuery, IReadOnlyList<string>>
{
    private const int DefaultTake = 50;
    private const int MaxTake = 200;

    private readonly ExpendableDbContext _dbContext;

    public GetProductArticlesQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<IReadOnlyList<string>> Handle(GetProductArticlesQuery query, CancellationToken cancellationToken)
    {
        var articleQuery = _dbContext.Products.AsNoTracking()
            .Select(p => p.Article)
            .Where(a => a != null && a != "");

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var pattern = $"%{query.Keyword.Trim()}%";
            articleQuery = articleQuery.Where(a => EF.Functions.ILike(a, pattern));
        }

        var take = query.Take is > 0 ? Math.Min(query.Take.Value, MaxTake) : DefaultTake;

        return await articleQuery
            .Distinct()
            .OrderBy(a => a)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
