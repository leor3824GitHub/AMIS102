using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.CreateJobOrder;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.InspectJobOrder;

public sealed class InspectJobOrderCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator) : ICommandHandler<InspectJobOrderCommand, JobOrderDto>
{
    public async ValueTask<JobOrderDto> Handle(InspectJobOrderCommand command, CancellationToken cancellationToken)
    {
        var jo = await dbContext.JobOrders
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Job order '{command.Id}' not found.");

        // Only the inspector assigned at creation may record inspection. Resolve the acting user's employee
        // id and let the aggregate enforce the match.
        var identityUserId = currentUser.GetUserId().ToString();
        var employee = await mediator.Send(new GetEmployeeReferenceByIdentityUserIdQuery(identityUserId), cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("No employee profile found for the current user. Cannot record inspection.");

        try
        {
            jo.Inspect(
                employee.Id,
                command.InvoiceNo,
                command.InvoiceDate,
                command.DateInspected,
                command.Findings,
                command.FoundInOrder);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new ForbiddenException(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            throw new CustomException(ex.Message, [], System.Net.HttpStatusCode.BadRequest);
        }

        jo.LastModifiedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreateJobOrderCommandHandler.MapToDto(jo);
    }
}
