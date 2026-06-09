using AMIS.Framework.Core.Context;
using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Products;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Products.RateProduct;

public sealed class RateProductCommandHandler : ICommandHandler<RateProductCommand>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public RateProductCommandHandler(ExpendableDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(RateProductCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();
        var tenantId = _currentUser.GetTenant()
            ?? throw new InvalidOperationException("Tenant context is required to rate a product.");

        // Tenant filter is applied automatically (IsMultiTenant), so this resolves the current
        // tenant's rating for this user + product only.
        var existing = await _dbContext.ProductRatings
            .FirstOrDefaultAsync(
                r => r.ProductId == command.ProductId && r.RaterUserId == userId,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var rating = ProductRating.Create(tenantId, command.ProductId, userId, command.Value);
            rating.CreatedBy = userId;
            await _dbContext.ProductRatings.AddAsync(rating, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            existing.UpdateValue(command.Value);
            existing.LastModifiedBy = userId;
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return default;
    }
}
