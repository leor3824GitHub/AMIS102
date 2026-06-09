using Mediator;

namespace AMIS.Modules.Expendable.Contracts.v1.Products;

/// <summary>Aggregated rating for a product: average score and number of ratings.</summary>
public record ProductRatingSummaryDto(
    Guid ProductId,
    double AverageRating,
    int RatingCount);

/// <summary>The current user's own rating for a product.</summary>
public record MyProductRatingDto(
    Guid ProductId,
    int Value);

/// <summary>Create or update the current user's rating (1-5) for a product.</summary>
public record RateProductCommand(
    Guid ProductId,
    int Value) : ICommand<Unit>;

/// <summary>Rating summaries for every product in the current tenant (for catalog cards).</summary>
public record GetProductRatingSummariesQuery() : IQuery<List<ProductRatingSummaryDto>>;

/// <summary>Rating summary (average + count) for a single product.</summary>
public record GetProductRatingSummaryQuery(Guid ProductId) : IQuery<ProductRatingSummaryDto>;

/// <summary>The current user's rating for a single product, or null if not yet rated.</summary>
public record GetMyProductRatingQuery(Guid ProductId) : IQuery<MyProductRatingDto?>;
