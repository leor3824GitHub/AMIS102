using System.Net;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Data;
using AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.UpdatePpmp;

public sealed class UpdatePpmpCommandHandler(
    ProcurementPlanningDbContext dbContext) : ICommandHandler<UpdatePpmpCommand, PpmpDto>
{
    public async ValueTask<PpmpDto> Handle(UpdatePpmpCommand command, CancellationToken cancellationToken)
    {
        var ppmp = await dbContext.Ppmps
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CustomException($"PPMP {command.Id} not found.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        ppmp.Update(
            command.FiscalYear,
            command.OfficeCode, command.EndUserUnit,
            command.PreparedById,
            command.Items.Select(PpmpMapper.ToItemData));

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PpmpMapper.ToDto(ppmp);
    }
}

