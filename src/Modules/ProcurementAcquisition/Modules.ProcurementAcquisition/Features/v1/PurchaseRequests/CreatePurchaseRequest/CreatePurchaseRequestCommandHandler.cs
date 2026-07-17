using AMIS.Framework.Core.Context;
using AMIS.Framework.Persistence.Sequencing;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Domain.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Shared;
using Mediator;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CreatePurchaseRequest;

public sealed class CreatePurchaseRequestCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator) : ICommandHandler<CreatePurchaseRequestCommand, PurchaseRequestDto>
{
    public async ValueTask<PurchaseRequestDto> Handle(CreatePurchaseRequestCommand command, CancellationToken cancellationToken)
    {
        var tenantId = GetRequiredTenantId();
        var now = DateTime.UtcNow;

        // Freeze the requester's name + designation from the authenticated user at create time
        // (the act of "requesting"), so reprints stay faithful to who requested and their title.
        var requesterId = currentUser.GetUserId();
        var requester = await SignatoryResolver
            .ResolveSignatoryAsync(currentUser, mediator, cancellationToken)
            .ConfigureAwait(false);

        // Allocate the PR number from a per-(tenant, year) counter guarded by xmin optimistic concurrency,
        // retrying on conflict. The formatted number carries the month cosmetically; the serial is year-scoped.
        var pr = await SequenceAllocator.AllocateAsync(
            dbContext, tenantId, sequenceKey: "PR", now.Year, month: 0,
            buildAndTrack: serial =>
            {
                var prNumber = $"{now.Year:D4}-{now.Month:D2}-{serial:D4}";

                var lineItems = command.LineItems.Select(li =>
                    new PurchaseRequestLineItemData(li.Quantity, li.UnitOfIssue, li.ItemDescription, li.EstimatedUnitCost, li.CatalogItemId, li.UacsObjectCode, li.StockNumber));

                var pr = PurchaseRequest.Create(
                    tenantId,
                    prNumber,
                    command.DepartmentId,
                    command.ResponsibilityCenterCode,
                    command.Purpose,
                    command.PrType,
                    command.Justification,
                    requester.Name,
                    command.SaiNumber,
                    command.SaiDate,
                    command.AlobsNumber,
                    command.AlobsDate,
                    lineItems,
                    requestedById: requesterId,
                    requestedByDesignation: requester.Designation,
                    category: command.Category);

                pr.CreatedBy = currentUser.GetUserId().ToString();
                dbContext.PurchaseRequests.Add(pr);
                return pr;
            },
            cancellationToken).ConfigureAwait(false);

        return MapToDto(pr);
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
                li.EstimatedUnitCost, li.EstimatedTotalCost, li.UacsObjectCode, li.CatalogItemId, li.StockNumber)).ToList(),
            pr.CreatedOnUtc,
            pr.CreatedBy,
            pr.LastModifiedOnUtc,
            FundsAvailableCertifiedById: pr.FundsAvailableCertifiedById,
            FundsAvailableCertifiedByName: pr.FundsAvailableCertifiedByName,
            FundsAvailableCertifiedOnUtc: pr.FundsAvailableCertifiedOnUtc,
            ApprovedById: pr.ApprovedById,
            ApprovedOnUtc: pr.ApprovedOnUtc,
            ReturnedReason: pr.ReturnedReason,
            ReturnedById: pr.ReturnedById,
            ReturnedByName: pr.ReturnedByName,
            ReturnedOnUtc: pr.ReturnedOnUtc,
            RejectionReason: pr.RejectionReason,
            RequestedById: pr.RequestedById,
            RequestedByDesignation: pr.RequestedByDesignation,
            ApprovedByDesignation: pr.ApprovedByDesignation,
            FundsAvailableCertifiedByDesignation: pr.FundsAvailableCertifiedByDesignation,
            Category: pr.Category);
    }
}
