using FSH.Framework.Core.Context;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using FSH.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.AmendPpmp;

public sealed class AmendPpmpCommandHandler(
    ProcurementPlanningDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<AmendPpmpCommand, PpmpDto>
{
    public async ValueTask<PpmpDto> Handle(AmendPpmpCommand command, CancellationToken cancellationToken)
    {
        var original = await dbContext.Ppmps
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == command.Id && x.IsCurrentVersion, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"PPMP {command.Id} not found or is not the current version.");

        var userId = currentUser.GetUserId().ToString();
        var amendment = original.CreateAmendment(command.AmendmentReason, userId);

        original.Supersede();

        amendment.CreatedBy = userId;
        dbContext.Ppmps.Add(amendment);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PpmpMapper.ToDto(amendment);
    }
}
