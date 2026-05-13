using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Data;
using AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.CreateUpdatePpmp;

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

