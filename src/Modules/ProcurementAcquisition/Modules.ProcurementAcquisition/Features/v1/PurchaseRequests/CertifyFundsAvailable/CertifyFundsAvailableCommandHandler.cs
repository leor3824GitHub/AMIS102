using AMIS.Framework.Core.Context;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CreatePurchaseRequest;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CertifyFundsAvailable;

public sealed class CertifyFundsAvailableCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CertifyFundsAvailableCommand, PurchaseRequestDto>
{
    public async ValueTask<PurchaseRequestDto> Handle(CertifyFundsAvailableCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var pr = await dbContext.PurchaseRequests
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Purchase request '{command.Id}' not found.");

        var uacsByLine = command.UacsByLine.ToDictionary(x => x.ItemNo, x => x.UacsObjectCode);
        var accountantId = currentUser.GetUserId();

        pr.CertifyFundsAvailable(
            accountantId,
            command.CertifiedByName,
            uacsByLine,
            command.AlobsNumber,
            command.AlobsDate);

        pr.LastModifiedBy = accountantId.ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreatePurchaseRequestCommandHandler.MapToDto(pr);
    }
}
