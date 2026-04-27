using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using FSH.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.SubmitPpmp;

public sealed class SubmitPpmpCommandHandler(
    ProcurementPlanningDbContext dbContext) : ICommandHandler<SubmitPpmpCommand, PpmpDto>
{
    public async ValueTask<PpmpDto> Handle(SubmitPpmpCommand command, CancellationToken cancellationToken)
    {
        var ppmp = await dbContext.Ppmps
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"PPMP {command.Id} not found.");

        ppmp.Submit();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PpmpMapper.ToDto(ppmp);
    }
}
