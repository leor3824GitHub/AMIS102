using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.ApprovePpmp;

public sealed class ApprovePpmpCommandHandler(
    ProcurementPlanningDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<ApprovePpmpCommand, PpmpDto>
{
    public async ValueTask<PpmpDto> Handle(ApprovePpmpCommand command, CancellationToken cancellationToken)
    {
        var ppmp = await dbContext.Ppmps
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CustomException($"PPMP {command.Id} not found.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        ppmp.Approve(currentUser.GetUserId());
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PpmpMapper.ToDto(ppmp);
    }
}

