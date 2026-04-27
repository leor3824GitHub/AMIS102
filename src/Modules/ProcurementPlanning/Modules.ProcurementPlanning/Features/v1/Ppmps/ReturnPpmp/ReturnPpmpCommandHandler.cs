using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using FSH.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.ReturnPpmp;

public sealed class ReturnPpmpCommandHandler(
    ProcurementPlanningDbContext dbContext) : ICommandHandler<ReturnPpmpCommand, PpmpDto>
{
    public async ValueTask<PpmpDto> Handle(ReturnPpmpCommand command, CancellationToken cancellationToken)
    {
        var ppmp = await dbContext.Ppmps
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"PPMP {command.Id} not found.");

        ppmp.Return(command.ReturnReason, command.ReturnedById);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PpmpMapper.ToDto(ppmp);
    }
}
