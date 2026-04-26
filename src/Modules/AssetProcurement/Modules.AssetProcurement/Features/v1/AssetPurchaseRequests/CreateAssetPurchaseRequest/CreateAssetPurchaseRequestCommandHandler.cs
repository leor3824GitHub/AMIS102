using FSH.Framework.Core.Context;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetPurchaseRequests;
using FSH.Modules.AssetProcurement.Data;
using FSH.Modules.AssetProcurement.Domain.AssetPurchaseRequests;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.CreateAssetPurchaseRequest;

public sealed class CreateAssetPurchaseRequestCommandHandler(
    AssetProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CreateAssetPurchaseRequestCommand, AssetPurchaseRequestDto>
{
    public async ValueTask<AssetPurchaseRequestDto> Handle(CreateAssetPurchaseRequestCommand command, CancellationToken cancellationToken)
    {
        var prNumber = await GeneratePrNumberAsync(cancellationToken).ConfigureAwait(false);

        var pr = AssetPurchaseRequest.Create(
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
            command.LineItems);

        pr.CreatedBy = currentUser.GetUserId().ToString();

        dbContext.AssetPurchaseRequests.Add(pr);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapToDto(pr);
    }

    private async Task<string> GeneratePrNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"APR-{year}-";

        var lastNumber = await dbContext.AssetPurchaseRequests
            .IgnoreQueryFilters()
            .Where(x => x.PrNumber.StartsWith(prefix))
            .Select(x => x.PrNumber)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var next = 1;
        if (lastNumber != null && int.TryParse(lastNumber[prefix.Length..], out var last))
            next = last + 1;

        return $"{prefix}{next:0000}";
    }

    internal static AssetPurchaseRequestDto MapToDto(AssetPurchaseRequest pr) =>
        new(pr.Id,
            pr.PrNumber,
            pr.PrDate,
            pr.SaiNumber,
            pr.SaiDate,
            pr.AlobsNumber,
            pr.AlobsDate,
            pr.DepartmentId,
            string.Empty,
            pr.Section,
            pr.Purpose,
            pr.PrType,
            pr.Justification,
            pr.Status,
            pr.RequestedById,
            string.Empty,
            pr.ApprovedById,
            null,
            pr.RejectionReason,
            pr.LineItems.Select(li => new AssetPurchaseRequestLineItemDto(
                li.ItemNo, li.ItemDescription, li.TechnicalSpecifications,
                li.Brand, li.Model, li.PropertyClassHint,
                li.Unit, li.Quantity, li.EstimatedUnitCost, li.EstimatedTotalCost)).ToList(),
            pr.CreatedOnUtc,
            pr.CreatedBy,
            pr.LastModifiedOnUtc);
}
