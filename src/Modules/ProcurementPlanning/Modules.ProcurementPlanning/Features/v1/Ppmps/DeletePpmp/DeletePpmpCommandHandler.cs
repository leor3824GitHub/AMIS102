using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.DeletePpmp;

public sealed class DeletePpmpCommandHandler(
    ProcurementPlanningDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<DeletePpmpCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeletePpmpCommand command, CancellationToken cancellationToken)
    {
        var ppmp = await dbContext.Ppmps
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CustomException($"PPMP {command.Id} not found.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        if (ppmp.Status != PpmpStatus.Draft)
            throw new CustomException(
                "Only Draft PPMPs can be deleted. Submitted or Approved PPMPs must be recalled or amended.",
                Enumerable.Empty<string>(),
                HttpStatusCode.Conflict);

        if (ppmp.PreviousVersionId is not null)
            throw new CustomException(
                "A draft amendment of an approved PPMP cannot be deleted.",
                Enumerable.Empty<string>(),
                HttpStatusCode.Conflict);

        ppmp.SoftDelete(currentUser.GetUserId().ToString());

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
