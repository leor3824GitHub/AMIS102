using AMIS.Framework.Core.Context;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Domain.PurchaseRequests;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CreatePurchaseRequest;

public sealed class CreatePurchaseRequestCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CreatePurchaseRequestCommand, PurchaseRequestDto>
{
    public async ValueTask<PurchaseRequestDto> Handle(CreatePurchaseRequestCommand command, CancellationToken cancellationToken)
    {
        var tenantId = GetRequiredTenantId();
        var now = DateTime.UtcNow;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var sequence = await dbContext.PrNumberSequences
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Year == now.Year, cancellationToken)
                .ConfigureAwait(false);

            if (sequence is null)
            {
                sequence = PrNumberSequence.Create(tenantId, now.Year);
                dbContext.PrNumberSequences.Add(sequence);
            }

            var serial = sequence.NextSerial();
            var prNumber = $"{now.Year:D4}-{now.Month:D2}-{serial:D4}";

            var lineItems = command.LineItems.Select(li =>
                (li.Quantity, li.UnitOfIssue, li.ItemDescription, li.EstimatedUnitCost));

            var pr = PurchaseRequest.Create(
                tenantId,
                prNumber,
                command.DepartmentId,
                command.ResponsibilityCenterCode,
                command.Purpose,
                command.PrType,
                command.Justification,
                command.RequestedByName,
                command.SaiNumber,
                command.SaiDate,
                command.AlobsNumber,
                command.AlobsDate,
                lineItems);

            pr.CreatedBy = currentUser.GetUserId().ToString();
            dbContext.PurchaseRequests.Add(pr);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return MapToDto(pr);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another request modified the sequence row between our read and save.
                // Clear tracked entities and retry with the latest sequence value.
                dbContext.ChangeTracker.Clear();
            }
        }

        throw new InvalidOperationException("Failed to allocate a unique PR number. Please try again.");
    }

    private string GetRequiredTenantId() =>
        currentUser.GetTenant()
        ?? dbContext.TenantInfo?.Identifier
        ?? throw new InvalidOperationException("Tenant ID required.");

    internal static PurchaseRequestDto MapToDto(PurchaseRequest pr)
    {
        return new PurchaseRequestDto(
            pr.Id,
            pr.PrNumber,
            pr.PrDate,
            pr.SaiNumber,
            pr.SaiDate,
            pr.AlobsNumber,
            pr.AlobsDate,
            pr.DepartmentId,
            string.Empty, // DepartmentName resolved by query handler
            pr.ResponsibilityCenterCode,
            pr.Purpose,
            pr.PrType,
            pr.Justification,
            pr.Status,
            pr.RequestedByName,
            pr.ApprovedByName,
            pr.LineItems.Select(li => new PurchaseRequestLineItemDto(
                li.ItemNo, li.Quantity, li.UnitOfIssue, li.ItemDescription,
                li.EstimatedUnitCost, li.EstimatedTotalCost)).ToList(),
            pr.CreatedOnUtc,
            pr.CreatedBy,
            pr.LastModifiedOnUtc);
    }
}
