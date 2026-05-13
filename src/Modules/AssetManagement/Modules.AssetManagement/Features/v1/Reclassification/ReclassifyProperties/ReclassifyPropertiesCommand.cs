using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.Reclassification.ReclassifyProperties;

/// <summary>
/// Applies the currently active capitalization threshold policy to all non-deleted
/// SemiExpendableProperty records, updating Category where it has changed.
///
/// This command is idempotent: running it twice against the same active policy
/// will reclassify 0 properties on the second run.
/// </summary>
public sealed record ReclassifyPropertiesCommand(string? Notes) : ICommand<ReclassifyPropertiesResult>;

public sealed record ReclassifyPropertiesResult(Guid RecordId, int TotalReclassified);

