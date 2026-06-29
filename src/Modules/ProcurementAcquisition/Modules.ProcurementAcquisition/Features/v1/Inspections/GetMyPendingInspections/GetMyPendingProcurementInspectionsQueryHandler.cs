using AMIS.Framework.Core.Context;
using AMIS.Framework.Shared.Inspections;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Inspections;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Inspections.GetMyPendingInspections;

/// <summary>
/// Aggregates the caller's pending Job Order and IAR inspections into the module-agnostic
/// <see cref="PendingInspectionItem"/> shape. Employee resolution mirrors
/// <c>InspectJobOrderCommandHandler</c>; an unlinked user yields an empty list (not an error).
/// </summary>
public sealed class GetMyPendingProcurementInspectionsQueryHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator)
    : IQueryHandler<GetMyPendingProcurementInspectionsQuery, IReadOnlyList<PendingInspectionItem>>
{
    public async ValueTask<IReadOnlyList<PendingInspectionItem>> Handle(
        GetMyPendingProcurementInspectionsQuery query, CancellationToken cancellationToken)
    {
        var identityUserId = currentUser.GetUserId().ToString();
        if (string.IsNullOrWhiteSpace(identityUserId) || identityUserId == Guid.Empty.ToString())
            return [];

        var employee = await mediator
            .Send(new GetEmployeeReferenceByIdentityUserIdQuery(identityUserId), cancellationToken)
            .ConfigureAwait(false);
        if (employee is null)
            return [];

        var me = employee.Id;

        // Job Orders issued to me as the C.O./F.O. inspector, still awaiting inspection. Project the raw
        // columns, then build the route strings in memory — string-concatenating a Guid inside the EF query
        // is not reliably translatable by the PostgreSQL provider.
        var jobOrders = await dbContext.JobOrders
            .AsNoTracking()
            .Where(jo => jo.Status == JobOrderStatus.Issued && jo.InspectorId == me)
            .Select(jo => new { jo.Id, jo.JoNumber, jo.SupplierName, jo.IssuedOnUtc, jo.CreatedOnUtc })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // IARs assigned to me as inspector, submitted and awaiting per-line inspection.
        var iars = await dbContext.InspectionAcceptanceReports
            .AsNoTracking()
            .Where(iar => iar.Status == InspectionAcceptanceReportStatus.PendingInspection && iar.InspectedById == me)
            .Select(iar => new { iar.Id, iar.IarNumber, iar.SupplierName, iar.SubmittedForInspectionOnUtc, iar.CreatedOnUtc })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return
        [
            .. jobOrders.Select(jo => new PendingInspectionItem(
                "JobOrder", jo.Id, jo.JoNumber, jo.SupplierName,
                jo.IssuedOnUtc ?? jo.CreatedOnUtc,
                $"/procurement/job-orders?inspect={jo.Id}")),
            .. iars.Select(iar => new PendingInspectionItem(
                "IAR", iar.Id, iar.IarNumber, iar.SupplierName,
                iar.SubmittedForInspectionOnUtc ?? iar.CreatedOnUtc,
                $"/procurement/inspection-acceptance-reports/{iar.Id}/inspect")),
        ];
    }
}
