using AMIS.Framework.Core.Context;
using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Products.GetMyProductRating;

public sealed class GetMyProductRatingQueryHandler
    : IQueryHandler<GetMyProductRatingQuery, MyProductRatingDto?>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyProductRatingQueryHandler(ExpendableDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<MyProductRatingDto?> Handle(
        GetMyProductRatingQuery query,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();

        var rating = await _dbContext.ProductRatings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.ProductId == query.ProductId && r.RaterUserId == userId,
                cancellationToken)
            .ConfigureAwait(false);

        return rating is null ? null : new MyProductRatingDto(rating.ProductId, rating.Value);
    }
}
