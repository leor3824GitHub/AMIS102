using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Persistence.Sequencing;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Domain.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CreatePurchaseOrder;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.CreateJobOrder;

public sealed class CreateJobOrderCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator) : ICommandHandler<CreateJobOrderCommand, JobOrderDto>
{
    public async ValueTask<JobOrderDto> Handle(CreateJobOrderCommand command, CancellationToken cancellationToken)
    {
        var tenantId = GetRequiredTenantId();

        if (!command.AllowDuplicate)
        {
            await EnsureNoDuplicateAsync(command, cancellationToken).ConfigureAwait(false);
        }

        // The C.O./F.O. Inspector is assigned now and printed on the JO form. Resolve the authoritative
        // name + designation from the employee directory so the request can't spoof them.
        var (inspectorName, inspectorDesignation) = await ResolveInspectorAsync(command.InspectorId, cancellationToken)
            .ConfigureAwait(false);

        // Works are not inventoried — JO line items are particulars (renovation/repair/fabrication) with no
        // StockNumber/CatalogItemId to recover, so they map straight through (unlike the PO line resolver).
        var lineItems = command.LineItems
            .Select(li => new JobOrderLineItemData(li.Unit, li.Description, li.Quantity, li.UnitCost))
            .ToList();

        // Allocate the JO number from a per-(tenant, year, month) counter guarded by xmin optimistic
        // concurrency, retrying on conflict — same race-safe pattern as PO/PR/IAR.
        var now = DateTime.UtcNow;
        var jo = await SequenceAllocator.AllocateAsync(
            dbContext, tenantId, sequenceKey: "JO", now.Year, now.Month,
            buildAndTrack: serial =>
            {
                var joNumber = $"{now.Year:D4}-{now.Month:D2}-{serial:D3}";

                var jo = JobOrder.Create(
                    tenantId,
                    joNumber,
                    command.PurchaseRequestId,
                    command.JobRequestNo,
                    command.RequisitioningOffice,
                    command.SupplierId,
                    command.SupplierName,
                    command.SupplierAddress,
                    command.SupplierTin,
                    command.ModeOfProcurement,
                    command.PlaceOfDelivery,
                    command.DateOfDelivery,
                    command.DeliveryTerm,
                    command.PaymentTerm,
                    command.FundCluster,
                    command.OursBursNumber,
                    command.InspectorId,
                    inspectorName,
                    inspectorDesignation,
                    lineItems);

                jo.CreatedBy = currentUser.GetUserId().ToString();
                dbContext.JobOrders.Add(jo);
                return jo;
            },
            cancellationToken).ConfigureAwait(false);

        return MapToDto(jo);
    }

    private async Task EnsureNoDuplicateAsync(CreateJobOrderCommand command, CancellationToken ct)
    {
        // Only request-driven JOs can collide on (PR, supplier); standalone JOs have no PR to key on.
        if (!command.PurchaseRequestId.HasValue)
        {
            return;
        }

        var existing = await dbContext.JobOrders
            .AsNoTracking()
            .Where(j => j.PurchaseRequestId == command.PurchaseRequestId.Value
                        && j.SupplierId == command.SupplierId
                        && j.Status != JobOrderStatus.Cancelled)
            .Select(j => new { j.JoNumber, j.Status })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            var message = $"A job order has already been created from this purchase request for this supplier. "
                + $"Existing JO: {existing.JoNumber} ({existing.Status}).";
            throw new CustomException(message, Enumerable.Empty<string>(), HttpStatusCode.Conflict);
        }
    }

    /// <summary>Resolves the assigned inspector's full name + designation from the employee directory.</summary>
    private async Task<(string Name, string? Designation)> ResolveInspectorAsync(Guid inspectorId, CancellationToken ct)
    {
        if (inspectorId == Guid.Empty)
            throw new CustomException("An inspector is required.", Enumerable.Empty<string>(), HttpStatusCode.BadRequest);

        var employee = await mediator.Send(new GetEmployeeReferenceByIdQuery(inspectorId), ct).ConfigureAwait(false)
            ?? throw new NotFoundException($"Inspector employee '{inspectorId}' not found.");

        return ($"{employee.FirstName} {employee.LastName}".Trim(), employee.PositionName);
    }

    private string GetRequiredTenantId() =>
        currentUser.GetTenant()
        ?? dbContext.TenantInfo?.Identifier
        ?? throw new InvalidOperationException("Tenant ID required.");

    internal static JobOrderDto MapToDto(JobOrder jo)
    {
        return new JobOrderDto(
            jo.Id,
            jo.JoNumber,
            jo.JoDate,
            jo.PurchaseRequestId,
            jo.JobRequestNo,
            jo.RequisitioningOffice,
            jo.SupplierId,
            jo.SupplierName,
            jo.SupplierAddress,
            jo.SupplierTin,
            jo.ModeOfProcurement,
            jo.PlaceOfDelivery,
            jo.DateOfDelivery,
            jo.DeliveryTerm,
            jo.PaymentTerm,
            jo.FundCluster,
            jo.OursBursNumber,
            jo.OursBursDate,
            jo.Status,
            jo.LineItems.Select(li => new JobOrderLineItemDto(
                li.ItemNo, li.Unit, li.Description, li.Quantity, li.UnitCost, li.Amount)).ToList(),
            jo.TotalAmount,
            // Reuse the shared peso-amount-to-words helper used by the PO printout.
            CreatePurchaseOrderCommandHandler.AmountToWords(jo.TotalAmount),
            jo.CreatedOnUtc,
            jo.CreatedBy,
            jo.LastModifiedOnUtc,
            jo.FundsAvailableCertifiedById,
            jo.FundsAvailableCertifiedByName,
            jo.FundsAvailableCertifiedOnUtc,
            jo.FundsAvailableCertifiedByDesignation,
            jo.IssuedById,
            jo.IssuedByName,
            jo.IssuedOnUtc,
            jo.IssuedByDesignation,
            jo.InspectorId,
            jo.InspectorName,
            jo.InspectorDesignation,
            jo.InspectedOnUtc,
            jo.InspectionInvoiceNo,
            jo.InspectionInvoiceDate,
            jo.DateInspected,
            jo.InspectionFindings,
            jo.FoundInOrder,
            jo.AcceptedById,
            jo.AcceptedOnUtc,
            jo.AcceptanceInvoiceNo,
            jo.DateReceived,
            jo.IsCompleteDelivery,
            jo.PartialDeliveryNote,
            jo.CancellationReason);
    }
}
