using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.CreateAssetIAR;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.RecordInspection;

public sealed class RecordIARInspectionCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator) : ICommandHandler<RecordIARInspectionCommand, AssetIARDto>
{
    public async ValueTask<AssetIARDto> Handle(RecordIARInspectionCommand command, CancellationToken cancellationToken)
    {
        var iar = await dbContext.AssetIARs
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset IAR '{command.Id}' not found.");

        var identityUserId = currentUser.GetUserId().ToString();
        var employee = await mediator.Send(new GetEmployeeReferenceByIdentityUserIdQuery(identityUserId), cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("No employee profile found for the current user. Cannot record inspection.");

        var actorId = employee.Id;
        iar.RecordInspection(actorId, command.Decisions);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var poNumber = await dbContext.PurchaseOrders
            .AsNoTracking()
            .Where(x => x.Id == iar.PurchaseOrderId)
            .Select(x => x.PoNumber)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? string.Empty;

        return CreateAssetIARCommandHandler.MapToDto(iar, poNumber);
    }
}
