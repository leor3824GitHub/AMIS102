using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PPEIssuanceReports.UpdatePPEIRDepreciation;

/// <summary>
/// Allows ASD/F.O. Accounting Unit to record accumulated depreciation and book value
/// for each item on a PPE Issuance Report after the transfer has been processed.
/// </summary>
public sealed record UpdatePPEIRDepreciationCommand(
    Guid PPEIRId,
    IReadOnlyList<PPEIRItemDepreciationRequest> Items) : ICommand<UpdatePPEIRDepreciationResult>;

public sealed record PPEIRItemDepreciationRequest(
    Guid ItemId,
    decimal AccumulatedDepreciation,
    decimal BookValue);

public sealed record UpdatePPEIRDepreciationResult(
    Guid PPEIRId,
    int ItemsUpdated);
