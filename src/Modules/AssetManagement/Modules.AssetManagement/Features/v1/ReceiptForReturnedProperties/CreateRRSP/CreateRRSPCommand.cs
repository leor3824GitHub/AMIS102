using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.ReceiptForReturnedProperties.CreateRRSP;

/// <summary>
/// Creates a Receipt for Returned Semi-Expendable Property (RRSP).
/// The RRSP cancels the referenced ICS and sets all issued property units back to Returned status.
/// The ICS must be Active at the time of this request.
/// All items on the ICS are returned — partial returns are not supported.
/// </summary>
public sealed record CreateRRSPCommand(
    string RRSPNo,
    DateOnly Date,
    Guid ICSId,
    string? FundCluster,
    Guid? ReceivedByEmployeeId,
    Guid ReturnedByEmployeeId,
    string? Remarks) : ICommand<CreateRRSPResult>;

public sealed record CreateRRSPResult(Guid RRSPId, string RRSPNo, int ItemCount);
