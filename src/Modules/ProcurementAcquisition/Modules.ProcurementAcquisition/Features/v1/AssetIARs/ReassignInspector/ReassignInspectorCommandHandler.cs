using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.ReassignInspector;

public sealed class ReassignInspectorCommandHandler(
    ProcurementDbContext dbContext) : ICommandHandler<ReassignInspectorCommand, AssetIARDto>
{
    public async ValueTask<AssetIARDto> Handle(ReassignInspectorCommand command, CancellationToken cancellationToken)
    {
        var iar = await dbContext.AssetIARs
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Asset IAR '{command.Id}' not found.");

        try { iar.ReassignInspector(command.NewInspectorId); }
        catch (InvalidOperationException ex)
        {
            throw new CustomException(ex.Message, [], System.Net.HttpStatusCode.BadRequest);
        }
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var poNumber = await dbContext.PurchaseOrders
            .AsNoTracking()
            .Where(x => x.Id == iar.PurchaseOrderId)
            .Select(x => x.PoNumber)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? string.Empty;

        return AssetIARMapper.ToDto(iar, poNumber);
    }
}
