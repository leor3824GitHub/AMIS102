using System.Net;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.SubmitPpmp;

public sealed class SubmitPpmpCommandHandler(
    ProcurementPlanningDbContext dbContext) : ICommandHandler<SubmitPpmpCommand, PpmpDto>
{
    public async ValueTask<PpmpDto> Handle(SubmitPpmpCommand command, CancellationToken cancellationToken)
    {
        var ppmp = await dbContext.Ppmps
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CustomException($"PPMP {command.Id} not found.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        ppmp.Submit();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PpmpMapper.ToDto(ppmp);
    }
}

