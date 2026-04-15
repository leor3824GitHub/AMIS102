using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.ThresholdPolicies.SetThresholdPolicy;

/// <summary>
/// Creates a new active CapitalizationThresholdPolicy and deactivates the previous one.
/// This is the only write operation on threshold policies — policies are never edited,
/// only superseded by a newer one. The full history is preserved for auditing.
/// </summary>
public sealed record SetThresholdPolicyCommand(
    decimal LowValueThreshold,
    decimal CapitalizationThreshold,
    DateOnly EffectiveDate,
    string? Reason) : ICommand<SetThresholdPolicyResult>;

public sealed record SetThresholdPolicyResult(
    Guid PolicyId,
    decimal LowValueThreshold,
    decimal CapitalizationThreshold,
    DateOnly EffectiveDate);
