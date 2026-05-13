using System.Net;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using FSH.Modules.ProcurementPlanning.Data;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.CreateUpdatePpmp;

public sealed class CreateUpdatePpmpCommandHandler(
    ProcurementPlanningDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CreateUpdatePpmpCommand, PpmpDto>
{
    public async ValueTask<PpmpDto> Handle(CreateUpdatePpmpCommand command, CancellationToken cancellationToken)
    {
        var original = await dbContext.Ppmps
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == command.Id && x.IsCurrentVersion, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CustomException($"PPMP {command.Id} not found or is not the current version.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        var userId = currentUser.GetUserId();
        var update = original.CreateUpdate(command.UpdateReason, userId);

        original.Supersede();

        update.CreatedBy = userId.ToString();
        dbContext.Ppmps.Add(update);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PpmpMapper.ToDto(update);
    }
}
