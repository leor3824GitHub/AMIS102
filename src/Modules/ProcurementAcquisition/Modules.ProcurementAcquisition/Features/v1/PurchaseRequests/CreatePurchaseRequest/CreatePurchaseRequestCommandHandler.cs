using FSH.Framework.Core.Context;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using FSH.Modules.ProcurementAcquisition.Data;
using FSH.Modules.ProcurementAcquisition.Domain.PurchaseRequests;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CreatePurchaseRequest;

public sealed class CreatePurchaseRequestCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CreatePurchaseRequestCommand, PurchaseRequestDto>
{
    public async ValueTask<PurchaseRequestDto> Handle(CreatePurchaseRequestCommand command, CancellationToken cancellationToken)
    {
        var tenantId = GetRequiredTenantId();
        var prNumber = await GeneratePrNumberAsync(tenantId, cancellationToken).ConfigureAwait(false);

        var lineItems = command.LineItems.Select(li =>
            (li.Quantity, li.UnitOfIssue, li.ItemDescription, li.EstimatedUnitCost));

        var pr = PurchaseRequest.Create(
            tenantId,
            prNumber,
            command.DepartmentId,
            command.Section,
            command.Purpose,
            command.PrType,
            command.Justification,
            command.RequestedById,
            command.SaiNumber,
            command.SaiDate,
            command.AlobsNumber,
            command.AlobsDate,
            lineItems);

        pr.CreatedBy = currentUser.GetUserId().ToString();

        dbContext.PurchaseRequests.Add(pr);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapToDto(pr);
    }

    private async Task<string> GeneratePrNumberAsync(string tenantId, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"PR-{year}-";

        var lastNumber = await dbContext.PurchaseRequests
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.PrNumber.StartsWith(prefix))
            .Select(x => x.PrNumber)
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
            pr.Section,
            pr.Purpose,
            pr.PrType,
            pr.Justification,
            pr.Status,
            pr.RequestedById,
            string.Empty, // RequestedByName resolved by query handler
            pr.ApprovedById,
            null,
            pr.LineItems.Select(li => new PurchaseRequestLineItemDto(
                li.ItemNo, li.Quantity, li.UnitOfIssue, li.ItemDescription,
                li.EstimatedUnitCost, li.EstimatedTotalCost)).ToList(),
            pr.CreatedOnUtc,
            pr.CreatedBy,
            pr.LastModifiedOnUtc);
    }
}
