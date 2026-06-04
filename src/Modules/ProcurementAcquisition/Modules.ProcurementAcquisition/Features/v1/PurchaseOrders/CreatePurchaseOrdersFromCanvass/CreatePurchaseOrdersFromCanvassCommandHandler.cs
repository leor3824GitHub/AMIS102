using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Domain.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CreatePurchaseOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CreatePurchaseOrdersFromCanvass;

/// <summary>
/// Fans a fully-awarded canvass out into one purchase order per winning supplier. Lines are grouped by their
/// per-line awarded supplier; each group becomes a PO carrying that supplier's won lines at the awarded unit
/// prices. Suppliers that already have a non-cancelled PO for the canvass are skipped, so re-running is safe.
/// </summary>
public sealed class CreatePurchaseOrdersFromCanvassCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CreatePurchaseOrdersFromCanvassCommand, IReadOnlyList<PurchaseOrderDto>>
{
    public async ValueTask<IReadOnlyList<PurchaseOrderDto>> Handle(
        CreatePurchaseOrdersFromCanvassCommand command, CancellationToken cancellationToken)
    {
        var tenantId = GetRequiredTenantId();

        var canvass = await dbContext.CanvassRequests
            .Include(x => x.Quotations)
            .FirstOrDefaultAsync(x => x.Id == command.CanvassRequestId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Canvass request '{command.CanvassRequestId}' not found.");

        if (canvass.Status != CanvassRequestStatus.Awarded)
            throw new CustomException(
                "Purchase orders can only be generated from an awarded canvass.",
                Enumerable.Empty<string>(), HttpStatusCode.Conflict);

        // Group the awarded lines by their winning supplier — each group becomes one PO.
        var groups = canvass.LineItems
            .Where(li => li.AwardedSupplierId is not null)
            .GroupBy(li => li.AwardedSupplierId!.Value)
            .ToList();

        if (groups.Count == 0)
            throw new CustomException(
                "This canvass has no awarded lines to turn into purchase orders.",
                Enumerable.Empty<string>(), HttpStatusCode.Conflict);

        // The PO inherits Asset/Supply and the screened StockNumber/CatalogItemId from the source PR (matched
        // by description), then guards Supply POs against any line that still can't resolve a StockNumber.
        var (category, prLines) = await PurchaseOrderLineResolver
            .LoadSourcePrAsync(dbContext, canvass.PurchaseRequestId, cancellationToken)
            .ConfigureAwait(false);

        // Suppliers already carrying a non-cancelled PO for this canvass — skip them (idempotent re-run).
        var alreadyOrderedSupplierIds = await dbContext.PurchaseOrders
            .AsNoTracking()
            .Where(p => p.CanvassRequestId == canvass.Id && p.Status != PurchaseOrderStatus.Cancelled)
            .Select(p => p.SupplierId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var alreadyOrdered = alreadyOrderedSupplierIds.ToHashSet();

        // Pre-resolve each group into supplier header + validated PO lines before the allocation loop, so a
        // resolution failure (e.g. a Supply line with no Stock No) fails fast without minting any PO numbers.
        var pending = new List<(Guid SupplierId, string Name, string Address, string? Tin, List<PurchaseOrderLineItemData> Lines)>();
        foreach (var group in groups)
        {
            if (alreadyOrdered.Contains(group.Key))
                continue;

            // Supplier header from the winning quotation (frozen supplier snapshot on the canvass).
            var quotation = canvass.Quotations.FirstOrDefault(q => q.SupplierId == group.Key)
                ?? throw new NotFoundException(
                    $"Winning quotation for supplier '{group.Key}' not found on the canvass.");

            var lineRequests = group
                .OrderBy(li => li.PrItemNo)
                .Select(li => new PurchaseOrderLineItemRequest(
                    StockNumber: null,
                    Unit: li.Unit,
                    Description: li.Description,
                    Quantity: li.Quantity,
                    UnitCost: li.AwardedUnitPrice ?? li.EstimatedUnitCost,
                    CatalogItemId: null));

            var lines = PurchaseOrderLineResolver.ResolveAndValidate(lineRequests, category, prLines);
            pending.Add((group.Key, quotation.SupplierName, quotation.SupplierAddress, quotation.TinNumber, lines));
        }

        if (pending.Count == 0)
            return [];

        var createdById = currentUser.GetUserId().ToString();
        var now = DateTime.UtcNow;

        // Allocate every PO number from the same per-(tenant, year, month) counter inside one retry loop, then
        // persist all POs in a single SaveChanges — same xmin-guarded pattern as the single-PO create path.
        for (var attempt = 0; attempt < SequenceAllocation.MaxAttempts; attempt++)
        {
            var sequence = await dbContext.PoNumberSequences
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    x => x.TenantId == tenantId && x.Year == now.Year && x.Month == now.Month,
                    cancellationToken)
                .ConfigureAwait(false);

            if (sequence is null)
            {
                sequence = PoNumberSequence.Create(tenantId, now.Year, now.Month);
                dbContext.PoNumberSequences.Add(sequence);
            }

            var created = new List<PurchaseOrder>(pending.Count);
            foreach (var p in pending)
            {
                var serial = sequence.NextSerial();
                var poNumber = $"{now.Year:D4}-{now.Month:D2}-{serial:D3}";

                var po = PurchaseOrder.Create(
                    tenantId,
                    poNumber,
                    canvass.PurchaseRequestId,
                    canvass.Id,
                    p.SupplierId,
                    p.Name,
                    p.Address,
                    p.Tin,
                    command.ModeOfProcurement,
                    command.PlaceOfDelivery,
                    command.DateOfDelivery,
                    command.DeliveryTerm,
                    command.PaymentTerm,
                    command.FundCluster,
                    command.OursBursNumber,
                    p.Lines,
                    category);

                po.CreatedBy = createdById;
                dbContext.PurchaseOrders.Add(po);
                created.Add(po);
            }

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return created.Select(CreatePurchaseOrderCommandHandler.MapToDto).ToList();
            }
            catch (DbUpdateException ex) when (SequenceAllocation.IsRetryableAllocationConflict(ex))
            {
                // Counter row advanced (xmin) or a generated number collided between read and save. Drop tracked
                // state and retry the whole batch with the latest serial.
                dbContext.ChangeTracker.Clear();
            }
        }

        throw new CustomException("Failed to allocate unique PO numbers. Please try again.",
            Enumerable.Empty<string>(), HttpStatusCode.Conflict);
    }

    private string GetRequiredTenantId() =>
        currentUser.GetTenant()
        ?? dbContext.TenantInfo?.Identifier
        ?? throw new InvalidOperationException("Tenant ID required.");
}
