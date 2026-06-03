using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.RecordInspection;

public sealed class RecordIARInspectionCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator) : ICommandHandler<RecordIARInspectionCommand, InspectionAcceptanceReportDto>
{
    public async ValueTask<InspectionAcceptanceReportDto> Handle(RecordIARInspectionCommand command, CancellationToken cancellationToken)
    {
        var iar = await dbContext.InspectionAcceptanceReports
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Asset IAR '{command.Id}' not found.");

        var identityUserId = currentUser.GetUserId().ToString();
        var employee = await mediator.Send(new GetEmployeeReferenceByIdentityUserIdQuery(identityUserId), cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("No employee profile found for the current user. Cannot record inspection.");

        var actorId = employee.Id;
        try { iar.RecordInspection(actorId, command.Decisions); }
        catch (UnauthorizedAccessException ex)
        {
            throw new ForbiddenException(ex.Message);
        }
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
