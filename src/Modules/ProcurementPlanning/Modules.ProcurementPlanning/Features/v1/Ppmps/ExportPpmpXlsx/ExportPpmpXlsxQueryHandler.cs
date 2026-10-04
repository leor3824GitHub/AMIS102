using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using Mediator;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.ExportPpmpXlsx;

public sealed class ExportPpmpXlsxQueryHandler(IMediator mediator) : IQueryHandler<ExportPpmpXlsxQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(ExportPpmpXlsxQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var ppmp = await mediator.Send(new GetPpmpQuery(query.PpmpId), cancellationToken).ConfigureAwait(false);

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        var preparer = ppmp.PreparedById == Guid.Empty
            ? null
            : await mediator.Send(new GetEmployeeReferenceByIdQuery(ppmp.PreparedById), cancellationToken).ConfigureAwait(false);

        return PpmpXlsxWorkbook.Build(ppmp, org?.Name, preparer);
    }
}
