using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.RenewICS;

/// <summary>
/// Renews an existing active ICS by issuing a new ICS for the same custodian and
/// the same property units.  The old ICS is marked <see cref="Domain.ICSStatus.Renewed"/>.
///
/// COA Circular 2022-004 Section 4.11: ICS must be renewed every three years while
/// the items remain with the same accountable officer.
/// </summary>
public sealed record RenewICSCommand(
    Guid OldICSId,
    string NewICSNo,
    DateOnly Date,
    Guid? IssuedFromEmployeeId) : ICommand<RenewICSResult>;

public sealed record RenewICSResult(
    Guid NewICSId,
    string NewICSNo,
    Guid OldICSId,
    int ItemCount);

