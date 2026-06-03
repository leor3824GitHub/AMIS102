using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Domain.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.CreateInspectionAcceptanceReport;

public sealed class CreateInspectionAcceptanceReportCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator) : ICommandHandler<CreateInspectionAcceptanceReportCommand, InspectionAcceptanceReportDto>
{
    public async ValueTask<InspectionAcceptanceReportDto> Handle(CreateInspectionAcceptanceReportCommand command, CancellationToken cancellationToken)
    {
        var tenantId = GetRequiredTenantId();

        var po = await dbContext.PurchaseOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.PurchaseOrderId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Purchase order '{command.PurchaseOrderId}' not found.");

        // An IAR receives goods against an issued PO; only Issued or PartiallyDelivered POs are eligible.
        // The UI already filters the picker to these, but enforce it server-side too so a direct API call
        // (or stale client) can't raise an IAR against a Draft/Submitted/Fulfilled/Cancelled PO.
        if (po.Status is not (PurchaseOrderStatus.Issued or PurchaseOrderStatus.PartiallyDelivered))
            throw new CustomException(
                $"Cannot create an IAR for purchase order '{po.PoNumber}' because it is {po.Status}. " +
                "Only Issued or Partially Delivered purchase orders can be received.",
                [], System.Net.HttpStatusCode.BadRequest);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var year = DateTime.UtcNow.Year;

            var sequence = await dbContext.IarNumberSequences
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Year == year, cancellationToken)
                .ConfigureAwait(false);

            if (sequence is null)
            {
                sequence = IarNumberSequence.Create(tenantId, year);
                dbContext.IarNumberSequences.Add(sequence);
            }

            var serial = sequence.NextSerial();
            var iarNumber = $"IAR-{year}-{serial:0000}";

            var iar = InspectionAcceptanceReport.Create(
                tenantId,
                iarNumber,
                command.PurchaseOrderId,
                po.SupplierId,
                po.SupplierName,
                command.InspectedById,
                command.ReceivedById,
                command.DeliveryReceiptNo,
                command.DeliveryDate,
                command.Remarks,
                command.LineItems,
                po.Category);

            iar.CreatedBy = currentUser.GetUserId().ToString();
            dbContext.InspectionAcceptanceReports.Add(iar);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                var (inspectorName, custodianName) = await InspectionAcceptanceReportMapper
                    .ResolveEmployeeNamesAsync(iar.InspectedById, iar.ReceivedById, mediator, cancellationToken)
                    .ConfigureAwait(false);

                return InspectionAcceptanceReportMapper.ToDto(iar, po.PoNumber, inspectorName, custodianName);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another request modified the sequence row between our read and save. Retry.
                dbContext.ChangeTracker.Clear();
            }
        }

        throw new CustomException("Failed to allocate a unique IAR number. Please try again.", [], System.Net.HttpStatusCode.Conflict);
    }

    private string GetRequiredTenantId() =>
        currentUser.GetTenant()
        ?? dbContext.TenantInfo?.Identifier
        ?? throw new InvalidOperationException("Tenant ID required.");
}
