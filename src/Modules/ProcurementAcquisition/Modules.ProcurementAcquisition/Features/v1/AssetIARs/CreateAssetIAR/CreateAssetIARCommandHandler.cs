using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Domain.AssetInspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.CreateAssetIAR;

public sealed class CreateAssetIARCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator) : ICommandHandler<CreateAssetIARCommand, AssetIARDto>
{
    public async ValueTask<AssetIARDto> Handle(CreateAssetIARCommand command, CancellationToken cancellationToken)
    {
        var tenantId = GetRequiredTenantId();

        var po = await dbContext.PurchaseOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.PurchaseOrderId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Purchase order '{command.PurchaseOrderId}' not found.");

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

            var iar = AssetInspectionAcceptanceReport.Create(
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
                command.LineItems);

            iar.CreatedBy = currentUser.GetUserId().ToString();
            dbContext.AssetIARs.Add(iar);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                var (inspectorName, custodianName) = await AssetIARMapper
                    .ResolveEmployeeNamesAsync(iar.InspectedById, iar.ReceivedById, mediator, cancellationToken)
                    .ConfigureAwait(false);

                return AssetIARMapper.ToDto(iar, po.PoNumber, inspectorName, custodianName);
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
