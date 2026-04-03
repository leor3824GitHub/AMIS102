using FSH.Framework.Core.Context;
using FSH.Modules.MasterData.Contracts.v1.ReportSignatories;
using FSH.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.ReportSignatories.DeleteReportSignatory;

public sealed class DeleteReportSignatoryCommandHandler(MasterDataDbContext db, ICurrentUser currentUser)
    : ICommandHandler<DeleteReportSignatoryCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteReportSignatoryCommand command, CancellationToken cancellationToken)
    {
        var entity = await db.ReportSignatories
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Report signatory {command.Id} not found.");

        entity.Delete(currentUser.GetUserId().ToString());

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
