using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using FSH.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.RecallPpmp;

public sealed class RecallPpmpCommandHandler(
    ProcurementPlanningDbContext dbContext) : ICommandHandler<RecallPpmpCommand, PpmpDto>
{
    public async ValueTask<PpmpDto> Handle(RecallPpmpCommand command, CancellationToken cancellationToken)
    {
        var ppmp = await dbContext.Ppmps
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CustomException($"PPMP {command.Id} not found.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        ppmp.Recall();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PpmpMapper.ToDto(ppmp);
    }
}
