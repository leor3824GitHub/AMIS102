using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.CancelInspectionAcceptanceReport;

public sealed class CancelInspectionAcceptanceReportCommandHandler(
    ProcurementDbContext dbContext) : ICommandHandler<CancelInspectionAcceptanceReportCommand, InspectionAcceptanceReportDto>
{
    public async ValueTask<InspectionAcceptanceReportDto> Handle(CancelInspectionAcceptanceReportCommand command, CancellationToken cancellationToken)
    {
        var iar = await dbContext.InspectionAcceptanceReports
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Asset IAR '{command.Id}' not found.");

        try { iar.Cancel(); }
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

        return InspectionAcceptanceReportMapper.ToDto(iar, poNumber);
    }
}
