using Mediator;

namespace AMIS.Modules.MasterData.Contracts.v1.CapitalizationThresholds;

public sealed record CapitalizationThresholdDto(
    Guid Id,
    string CircularName,
    string Description,
    decimal CapitalizationAmount,
    decimal SemiExpendableLowValueThreshold,
    DateOnly EffectivityDate,
    bool IsActive);

// Queries
public sealed record GetActiveCapitalizationThresholdQuery() : IQuery<CapitalizationThresholdDto?>;

public sealed record GetCapitalizationThresholdsQuery() : IQuery<IReadOnlyList<CapitalizationThresholdDto>>;

// Commands
public sealed record CreateCapitalizationThresholdCommand(
    string CircularName,
    string Description,
    decimal CapitalizationAmount,
    decimal SemiExpendableLowValueThreshold,
    DateOnly EffectivityDate) : ICommand<Guid>;

public sealed record UpdateCapitalizationThresholdCommand(
    Guid Id,
    string CircularName,
    string Description,
    decimal CapitalizationAmount,
    decimal SemiExpendableLowValueThreshold,
    DateOnly EffectivityDate) : ICommand<CapitalizationThresholdDto>;

public sealed record SetActiveCapitalizationThresholdCommand(Guid Id) : ICommand;

