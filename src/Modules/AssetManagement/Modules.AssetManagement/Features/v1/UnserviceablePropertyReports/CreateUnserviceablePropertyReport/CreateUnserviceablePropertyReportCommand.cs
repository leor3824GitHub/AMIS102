using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.UnserviceablePropertyReports.CreateUnserviceablePropertyReport;

/// <summary>
/// Creates an Inspection and Inventory Report of Unserviceable Semi-Expendable
/// Property (IIRUSP).  Each listed TangibleInventoryItem is snapshotted into a line.
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
    Guid TangibleInventoryItemId,
    string? ConditionRemarks);

public sealed record CreateUnserviceablePropertyReportResult(Guid ReportId, string ReportNo, int ItemCount);
