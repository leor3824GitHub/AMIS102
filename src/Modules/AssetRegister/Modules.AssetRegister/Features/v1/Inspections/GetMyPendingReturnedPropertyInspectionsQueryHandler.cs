using AMIS.Framework.Core.Context;
using AMIS.Framework.Shared.Inspections;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Inspections;
using AMIS.Modules.AssetRegister.Contracts.v1.ReturnedProperty;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Features.v1.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Inspections;

/// <summary>
/// Returns the caller's Pending return-requests (where they are the nominated inspector) as module-agnostic
/// <see cref="PendingInspectionItem"/>s. Employee resolution reuses <see cref="CurrentEmployeeResolver"/>;
/// an unlinked user yields an empty list (not an error).
/// </summary>
public sealed class GetMyPendingReturnedPropertyInspectionsQueryHandler(
    AssetRegisterDbContext db,
    ICurrentUser currentUser,
    IMediator mediator)
    : IQueryHandler<GetMyPendingReturnedPropertyInspectionsQuery, IReadOnlyList<PendingInspectionItem>>
{
    public async ValueTask<IReadOnlyList<PendingInspectionItem>> Handle(
        GetMyPendingReturnedPropertyInspectionsQuery query, CancellationToken cancellationToken)
    {
        var employee = await CurrentEmployeeResolver.TryResolveAsync(currentUser, mediator, cancellationToken)
            .ConfigureAwait(false);
        if (employee is null)
            return [];

        var me = employee.Id;

        // The receipt number is null until acceptance, so a Pending row has none — the accountability
        // document number is the only stable reference at this stage.
        var rows = await db.ReturnedPropertyReceipts
            .AsNoTracking()
            .Where(r => r.Status == ReturnedPropertyReceiptStatus.Pending && r.AssignedInspector.EmployeeId == me)
            .Select(r => new
            {
                r.Id,
                r.AccountabilityDocumentNo,
                r.ReceiptType,
                ItemCount = r.Items.Count,
                r.CreatedOnUtc,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. rows.Select(r => new PendingInspectionItem(
            "ReturnedProperty",
            r.Id,
            r.AccountabilityDocumentNo,
            $"{r.ReceiptType} · {r.ItemCount} item(s)",
            r.CreatedOnUtc,
            "/asset-register/returned-property?inspect=" + r.Id))];
    }
}
