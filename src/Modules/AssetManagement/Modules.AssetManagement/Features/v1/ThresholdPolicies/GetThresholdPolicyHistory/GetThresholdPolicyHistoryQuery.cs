using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.ThresholdPolicies.GetThresholdPolicyHistory;

public sealed record GetThresholdPolicyHistoryQuery(
    int PageNumber = 1,
    int PageSize = 20) : IQuery<PagedThresholdPolicyHistoryResponse>;

public sealed record PagedThresholdPolicyHistoryResponse(
    IReadOnlyList<ThresholdPolicyHistoryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record ThresholdPolicyHistoryDto(
    Guid Id,
    decimal LowValueThreshold,
    decimal CapitalizationThreshold,
    DateOnly EffectiveDate,
    bool IsActive,
    string? Reason,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy);
