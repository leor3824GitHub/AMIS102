using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.ThresholdPolicies.GetActiveThresholdPolicy;

public sealed record GetActiveThresholdPolicyQuery : IQuery<ThresholdPolicyDto>;

public sealed record ThresholdPolicyDto(
    Guid Id,
    decimal LowValueThreshold,
    decimal CapitalizationThreshold,
    DateOnly EffectiveDate,
    bool IsActive,
    string? Reason,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy);
