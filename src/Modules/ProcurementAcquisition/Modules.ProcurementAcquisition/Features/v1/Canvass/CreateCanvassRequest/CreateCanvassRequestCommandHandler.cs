using FSH.Framework.Core.Context;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using FSH.Modules.ProcurementAcquisition.Data;
using FSH.Modules.ProcurementAcquisition.Domain.Canvass;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.Canvass.CreateCanvassRequest;

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
            ?? throw new KeyNotFoundException($"Purchase request '{command.PurchaseRequestId}' not found.");

        if (pr.Status != PurchaseRequestStatus.Approved)
            throw new InvalidOperationException("Can only create a canvass request for an Approved purchase request.");

        var alreadyExists = await dbContext.CanvassRequests
            .AnyAsync(x => x.PurchaseRequestId == command.PurchaseRequestId, cancellationToken)
            .ConfigureAwait(false);

        if (alreadyExists)
            throw new InvalidOperationException("A canvass request already exists for this purchase request.");

        var rivNumber = await GenerateRivNumberAsync(tenantId, cancellationToken).ConfigureAwait(false);

        var canvass = CanvassRequest.Create(tenantId, rivNumber, command.PurchaseRequestId, command.ReturnDeadline);
        canvass.CreatedBy = currentUser.GetUserId().ToString();

        dbContext.CanvassRequests.Add(canvass);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapToDto(canvass, pr.PrNumber);
    }

    private async Task<string> GenerateRivNumberAsync(string tenantId, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"RIV-{year}-";

        var lastNumber = await dbContext.CanvassRequests
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.RivNumber.StartsWith(prefix))
            .Select(x => x.RivNumber)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var next = 1;
        if (lastNumber != null && int.TryParse(lastNumber[prefix.Length..], out var last))
        {
            next = last + 1;
        }

        return $"{prefix}{next:0000}";
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
            canvass.CreatedBy);
    }
}
