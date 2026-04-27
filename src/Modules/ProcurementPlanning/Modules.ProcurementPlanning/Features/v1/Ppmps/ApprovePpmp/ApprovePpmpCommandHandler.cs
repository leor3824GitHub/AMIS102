using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using FSH.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.ApprovePpmp;

public sealed class ApprovePpmpCommandHandler(
    ProcurementPlanningDbContext dbContext) : ICommandHandler<ApprovePpmpCommand, PpmpDto>
{
    public async ValueTask<PpmpDto> Handle(ApprovePpmpCommand command, CancellationToken cancellationToken)
    {
        var ppmp = await dbContext.Ppmps
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"PPMP {command.Id} not found.");

        ppmp.Approve(command.ApprovedById);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PpmpMapper.ToDto(ppmp);
    }
}
