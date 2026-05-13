using AMIS.Modules.AssetManagement.Domain;
using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.GetExpiringICS;

/// <summary>
/// Returns Active ICS records whose <see cref="InventoryCustodianSlip.ExpiresOn"/> falls
/// within the next <see cref="DaysAhead"/> days (inclusive of today).
/// Used to surface renewal reminders to the front-end.
/// </summary>
public sealed record GetExpiringICSQuery(
    int DaysAhead   = 30,
    int PageNumber  = 1,
    int PageSize    = 20) : IQuery<PagedExpiringICSResponse>;

public sealed record ExpiringICSSummaryDto(
    Guid Id,
    string ICSNo,
    DateOnly Date,
    DateOnly ExpiresOn,
    string Category,
    Guid ReceivedByEmployeeId,
    Guid? IssuedFromEmployeeId,
    string? FundCluster,
    int ItemCount);

public sealed record PagedExpiringICSResponse(
    IReadOnlyList<ExpiringICSSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

