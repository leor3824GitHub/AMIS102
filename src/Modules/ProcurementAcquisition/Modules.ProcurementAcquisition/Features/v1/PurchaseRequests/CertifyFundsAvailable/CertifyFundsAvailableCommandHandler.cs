using AMIS.Framework.Core.Context;
using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CreatePurchaseRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CertifyFundsAvailable;

public sealed class CertifyFundsAvailableCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator,
    IEventBus eventBus) : ICommandHandler<CertifyFundsAvailableCommand, PurchaseRequestDto>
{
    public async ValueTask<PurchaseRequestDto> Handle(CertifyFundsAvailableCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var pr = await dbContext.PurchaseRequests
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new AMIS.Framework.Core.Exceptions.NotFoundException($"Purchase request '{command.Id}' not found.");

        var uacsByLine = command.UacsByLine.ToDictionary(x => x.ItemNo, x => x.UacsObjectCode);
        var accountantId = currentUser.GetUserId();
        // Signatory name + designation come from the authenticated identity, never from the request body.
        var certifier = await SignatoryResolver.ResolveSignatoryAsync(currentUser, mediator, cancellationToken).ConfigureAwait(false);

        pr.CertifyFundsAvailable(
            accountantId,
            certifier.Name,
            uacsByLine,
            command.AlobsNumber,
            command.AlobsDate,
            certifiedByDesignation: certifier.Designation);

        pr.LastModifiedBy = accountantId.ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Publish UACS-certified event for lines that reference a catalog item — AssetRegister consumes this
        // to back-fill UACS on Draft catalog rows. Lines with no CatalogItemId are skipped (free-text PRs).
        var lines = pr.LineItems
            .Where(li => li.CatalogItemId is not null && li.CatalogItemId != Guid.Empty
                         && !string.IsNullOrWhiteSpace(li.UacsObjectCode))
            .Select(li => new PurchaseRequestUacsCertifiedEventLine(li.ItemNo, li.CatalogItemId!.Value, li.UacsObjectCode!))
            .ToList();

        if (lines.Count > 0)
        {
            var integrationEvent = new PurchaseRequestUacsCertifiedEvent(
                PurchaseRequestId: pr.Id,
                PrNumber: pr.PrNumber,
                Lines: lines,
                TenantId: dbContext.TenantInfo?.Identifier);

            await eventBus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
        }

        return CreatePurchaseRequestCommandHandler.MapToDto(pr);
    }
}
