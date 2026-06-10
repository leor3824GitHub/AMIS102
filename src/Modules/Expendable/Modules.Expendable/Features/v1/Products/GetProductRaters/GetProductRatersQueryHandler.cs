using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Identity.Contracts.DTOs;
using AMIS.Modules.Identity.Contracts.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Products.GetProductRaters;

public sealed class GetProductRatersQueryHandler
    : IQueryHandler<GetProductRatersQuery, List<ProductRaterDto>>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly IUserService _userService;

    public GetProductRatersQueryHandler(ExpendableDbContext dbContext, IUserService userService)
    {
        _dbContext = dbContext;
        _userService = userService;
    }

    public async ValueTask<List<ProductRaterDto>> Handle(
        GetProductRatersQuery query,
        CancellationToken cancellationToken)
    {
        // Tenant filter is applied automatically (IsMultiTenant).
        var ratings = await _dbContext.ProductRatings
            .AsNoTracking()
            .Where(r => r.ProductId == query.ProductId)
            .Select(r => new
            {
                r.RaterUserId,
                r.Value,
                RatedOnUtc = r.LastModifiedOnUtc ?? r.CreatedOnUtc
            })
            .OrderByDescending(r => r.RatedOnUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (ratings.Count == 0)
            return [];

        // Resolve display names in one pass to avoid an N+1 against Identity.
        var users = await _userService.GetListAsync(cancellationToken).ConfigureAwait(false);
        var namesById = users
            .Where(u => u.Id is not null)
            .ToDictionary(u => u.Id!, BuildDisplayName, StringComparer.Ordinal);

        return ratings
            .Select(r => new ProductRaterDto(
                r.RaterUserId,
                namesById.TryGetValue(r.RaterUserId, out var name) ? name : "Unknown user",
                r.Value,
                r.RatedOnUtc))
            .ToList();
    }

    private static string BuildDisplayName(UserDto user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;

        return user.UserName ?? user.Email ?? "Unknown user";
    }
}