using Mediator;

namespace AMIS.Modules.AssetRegister.Contracts.v1.Reports;

/// <summary>
/// One chronological movement on an asset's Property Card (COA §6.3.1.a): acquisition →
/// issue/transfer → return → unserviceable/disposal → loss/recovery. Projected on demand from the
/// source documents (no separate stored ledger).
/// </summary>
public sealed record PropertyCardRowDto(
    DateOnly Date,
    AssetMovementType MovementType,
    MovementSource Source,
    string? DocumentNo,
    Guid? DocumentId,
    string? Party,
    decimal? Amount,
    string? Remarks);

public sealed record PropertyCardDto(
    Guid AssetRegistryId,
    string PropertyNo,
    string Description,
    AssetType AssetType,
    string Unit,
    DateOnly AcquisitionDate,
    decimal AcquisitionCost,
    LifecycleState CurrentState,
    IReadOnlyCollection<PropertyCardRowDto> Movements);

public sealed record GetPropertyCardQuery(string PropertyNo) : IQuery<PropertyCardDto?>;
