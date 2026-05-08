using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using FSH.Modules.ProcurementPlanning.Data;
using FSH.Modules.ProcurementPlanning.Features.v1.Ppmps;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.GetPpmp;

public sealed class GetPpmpQueryHandler(
    ProcurementPlanningDbContext dbContext) : IQueryHandler<GetPpmpQuery, PpmpDto>
{
    public async ValueTask<PpmpDto> Handle(GetPpmpQuery query, CancellationToken cancellationToken)
    {
        var ppmp = await dbContext.Ppmps
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CustomException($"PPMP {query.Id} not found.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        return PpmpMapper.ToDto(ppmp);
    }
}
