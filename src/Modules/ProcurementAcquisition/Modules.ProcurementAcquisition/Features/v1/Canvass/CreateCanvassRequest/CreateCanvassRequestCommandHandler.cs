using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Domain.Canvass;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.CreateCanvassRequest;

public sealed class CreateCanvassRequestCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CreateCanvassRequestCommand, CanvassRequestDto>
{
    public async ValueTask<CanvassRequestDto> Handle(CreateCanvassRequestCommand command, CancellationToken cancellationToken)
    {
        var tenantId = GetRequiredTenantId();

        var pr = await dbContext.PurchaseRequests
            .FirstOrDefaultAsync(x => x.Id == command.PurchaseRequestId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new AMIS.Framework.Core.Exceptions.NotFoundException($"Purchase request '{command.PurchaseRequestId}' not found.");

        if (pr.Status != PurchaseRequestStatus.Approved)
            throw new InvalidOperationException("Can only create a canvass request for an Approved purchase request.");

        // Resolve requested PR lines and validate they exist on the PR.
        var requestedItemNos = command.PrItemNos.Distinct().ToList();
        var prLinesByItemNo = pr.LineItems.ToDictionary(li => li.ItemNo);

        var unknown = requestedItemNos.Where(no => !prLinesByItemNo.ContainsKey(no)).ToList();
        if (unknown.Count > 0)
            throw new CustomException(
                $"Item No(s) {string.Join(", ", unknown)} do not exist on purchase request {pr.PrNumber}.",
                Enumerable.Empty<string>(), HttpStatusCode.BadRequest);

        // Partition check: a PR line may belong to at most one non-cancelled canvass.
        var activeCanvasses = await dbContext.CanvassRequests
            .AsNoTracking()
            .Where(x => x.PurchaseRequestId == command.PurchaseRequestId
                        && x.Status != CanvassRequestStatus.Cancelled)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var coveredBy = new Dictionary<int, string>();
        foreach (var c in activeCanvasses)
            foreach (var no in c.CoveredItemNos)
                coveredBy.TryAdd(no, c.RivNumber);

        var conflicts = requestedItemNos
            .Where(coveredBy.ContainsKey)
            .Select(no => $"Item {no} (already on {coveredBy[no]})")
            .ToList();
        if (conflicts.Count > 0)
            throw new CustomException(
                $"The following line item(s) are already covered by another canvass: {string.Join("; ", conflicts)}.",
                Enumerable.Empty<string>(), HttpStatusCode.Conflict);

        // Line items are stable across number-allocation retries, so build them once, before the loop.
        var lineItems = requestedItemNos
            .OrderBy(no => no)
            .Select(no => prLinesByItemNo[no])
            .Select(li => new CanvassRequestLineItemData(
                li.ItemNo, li.ItemDescription, li.UnitOfIssue, li.Quantity, li.EstimatedUnitCost, li.CatalogItemId, li.UacsObjectCode))
            .ToList();

        // Allocate the RIV number from a per-(tenant, year) counter guarded by xmin optimistic concurrency,
        // retrying on conflict — same race-safe pattern as PR/IAR/PO. Replaces the old "max(RivNumber) + 1"
        // scan, which could mint duplicate numbers (or fail the unique index) under concurrent creation.
        var year = DateTime.UtcNow.Year;
        for (var attempt = 0; attempt < SequenceAllocation.MaxAttempts; attempt++)
        {
            var sequence = await dbContext.RivNumberSequences
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Year == year, cancellationToken)
                .ConfigureAwait(false);

            if (sequence is null)
            {
                sequence = RivNumberSequence.Create(tenantId, year);
                dbContext.RivNumberSequences.Add(sequence);
            }

            var serial = sequence.NextSerial();
            var rivNumber = $"RIV-{year}-{serial:0000}";

            var canvass = CanvassRequest.Create(tenantId, rivNumber, command.PurchaseRequestId, command.ReturnDeadline, lineItems);
            canvass.CreatedBy = currentUser.GetUserId().ToString();
            dbContext.CanvassRequests.Add(canvass);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return MapToDto(canvass, pr.PrNumber);
            }
            catch (DbUpdateException ex) when (SequenceAllocation.IsRetryableAllocationConflict(ex))
            {
                // Counter row advanced (xmin) or the generated number collided with an existing canvass (unique
                // violation) between our read and save. Drop tracked state and retry with the latest value.
                dbContext.ChangeTracker.Clear();
            }
        }

        throw new CustomException("Failed to allocate a unique RIV number. Please try again.",
            Enumerable.Empty<string>(), HttpStatusCode.Conflict);
    }

    private string GetRequiredTenantId() =>
        currentUser.GetTenant()
        ?? dbContext.TenantInfo?.Identifier
        ?? throw new InvalidOperationException("Tenant ID required.");

    internal static CanvassRequestDto MapToDto(CanvassRequest canvass, string prNumber)
    {
        return new CanvassRequestDto(
            canvass.Id,
            canvass.RivNumber,
            canvass.PurchaseRequestId,
            prNumber,
            canvass.ReturnDeadline,
            canvass.Status,
            canvass.AwardedSupplierId,
            null,
            canvass.Quotations.Select(q => new CanvassQuotationDto(
                q.Id,
                q.SupplierId,
                q.SupplierName,
                q.SupplierAddress,
                q.TinNumber,
                q.QuotationDate,
                q.DeliveryTerms,
                q.IsAwarded,
                q.LineItems.Select(li => new CanvassQuotationLineItemDto(
                    li.ItemNo, li.Description, li.Unit, li.Quantity, li.UnitPrice, li.Total)).ToList()
            )).ToList(),
            canvass.CreatedOnUtc,
            canvass.CreatedBy,
            canvass.LineItems.Select(li => new CanvassLineItemDto(
                li.PrItemNo, li.Description, li.Unit, li.Quantity, li.EstimatedUnitCost, li.EstimatedTotalCost)).ToList(),
            AwardSignatories: canvass.AwardSignatories
                .Select(s => new CanvassAwardSignatoryDto(s.SortOrder, s.Name, s.Role)).ToList());
    }
}

