using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PPEReceivingReports.CreatePPERR;

public sealed record CreatePPERRCommand(
    string PPERRNo,
    DateOnly Date,
    string ReceivedFrom,
    string Address,
    PPEReceiptNature ReceiptNature,
    Guid ReceivedByEmployeeId,
    Guid NotedByEmployeeId,
    IReadOnlyList<CreatePPERRItemRequest> Items) : ICommand<CreatePPERRResult>;

public sealed record CreatePPERRItemRequest(
    /// <summary>
    /// 2-char COA GAM Annex A classification code (e.g. "OE").
    /// When provided together with ItemCode, the property code is auto-generated.
    /// Leave null to use the explicit PropertyCode value instead.
    /// </summary>
    string? ClassCode,
    /// <summary>1-char category code (e.g. "C" for Computer).</summary>
    string? ItemCode,
    /// <summary>
    /// Explicit property code. Required when ClassCode/ItemCode are not provided.
    /// Ignored when ClassCode + ItemCode are both supplied (code will be auto-generated).
    /// </summary>
    string? PropertyCode,
    string Description,
    string? SerialNumber,
    DateOnly DateAcquired,
    int Quantity,
    decimal UnitCost,
    int EstimatedUsefulLifeYears);

public sealed record CreatePPERRResult(
    Guid PPERRId,
    string PPERRNo,
    int PPEItemsCreated);
