using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.CreateAssetIAR;
using Mediator;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.SubmitForInspection;

public sealed class SubmitIARForInspectionCommandHandler(
    ProcurementDbContext dbContext) : ICommandHandler<SubmitIARForInspectionCommand, AssetIARDto>
{
    public async ValueTask<AssetIARDto> Handle(SubmitIARForInspectionCommand command, CancellationToken cancellationToken)
    {
        var iar = await dbContext.AssetIARs
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException(
                $"Asset IAR '{command.Id}' not found.",
                Enumerable.Empty<string>(),
                HttpStatusCode.NotFound);

        try
        {
            iar.SubmitForInspection();
        }
        catch (InvalidOperationException ex)
        {
            throw new CustomException(ex.Message, Enumerable.Empty<string>(), HttpStatusCode.BadRequest);
        }

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
