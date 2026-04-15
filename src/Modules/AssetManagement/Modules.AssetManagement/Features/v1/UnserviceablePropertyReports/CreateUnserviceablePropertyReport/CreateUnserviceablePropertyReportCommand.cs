using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.UnserviceablePropertyReports.CreateUnserviceablePropertyReport;

/// <summary>
/// Creates an Inspection and Inventory Report of Unserviceable Semi-Expendable
/// Property (IIRUSP).
/// All listed properties are set to PropertyStatus.Disposed.
/// Properties must be in OnHand, Returned, or LostStolenDamaged status.
/// If the property is Issued, the employee must first return it via RRSP.
/// </summary>
public sealed record CreateUnserviceablePropertyReportCommand(
    string ReportNo,
    DateOnly Date,
    DisposalMethod DisposalMethod,
    string? FundCluster,
    Guid? InspectedByEmployeeId,
    Guid? ApprovedByEmployeeId,
    string? Remarks,
    IReadOnlyList<CreateUnserviceablePropertyItemRequest> Items) : ICommand<CreateUnserviceablePropertyReportResult>;

public sealed record CreateUnserviceablePropertyItemRequest(
    Guid SemiExpendablePropertyId,
    string? ConditionRemarks);

public sealed record CreateUnserviceablePropertyReportResult(Guid ReportId, string ReportNo, int ItemCount);
