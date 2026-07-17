using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Persistence.Sequencing;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Domain.Canvass;
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

        // A PR may have several canvasses covering identical lines (one RIV per supplier group). Coverage no longer
        // partitions the PR's lines at creation — the "one canvass per awarded line" invariant is instead enforced
        // at award time. So no create-time conflict check: the same line may be canvassed on multiple canvasses.

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
        var canvass = await SequenceAllocator.AllocateAsync(
            dbContext, tenantId, sequenceKey: "RIV", year, month: 0,
            buildAndTrack: serial =>
            {
                var rivNumber = $"RIV-{year}-{serial:0000}";

                var canvass = CanvassRequest.Create(tenantId, rivNumber, command.PurchaseRequestId, command.ReturnDeadline, lineItems);
                canvass.CreatedBy = currentUser.GetUserId().ToString();
                dbContext.CanvassRequests.Add(canvass);
                return canvass;
            },
            cancellationToken).ConfigureAwait(false);

        return MapToDto(canvass, pr.PrNumber);
    }

    private string GetRequiredTenantId() =>
        currentUser.GetTenant()
        ?? dbContext.TenantInfo?.Identifier
        ?? throw new InvalidOperationException("Tenant ID required.");

    internal static CanvassRequestDto MapToDto(CanvassRequest canvass, string prNumber)
    {
        // Per-line awarded supplier names come from the canvass's own quotations (frozen suppliers).
        var supplierNameById = canvass.Quotations
            .GroupBy(q => q.SupplierId)
            .ToDictionary(g => g.Key, g => g.First().SupplierName);

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
                    li.ItemNo, li.PrItemNo, li.Description, li.Unit, li.Quantity, li.UnitPrice, li.Total)).ToList()
            )).ToList(),
            canvass.CreatedOnUtc,
            canvass.CreatedBy,
            canvass.LineItems.Select(li => new CanvassLineItemDto(
                li.PrItemNo, li.Description, li.Unit, li.Quantity, li.EstimatedUnitCost, li.EstimatedTotalCost,
                li.AwardedQuotationId, li.AwardedSupplierId,
                li.AwardedSupplierId is { } sid && supplierNameById.TryGetValue(sid, out var name) ? name : null,
                li.AwardedUnitPrice)).ToList(),
            AwardSignatories: canvass.AwardSignatories
                .Select(s => new CanvassAwardSignatoryDto(s.SortOrder, s.Name, s.Role)).ToList());
    }
}

