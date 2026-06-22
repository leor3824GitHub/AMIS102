using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.AssetRegister.Domain.Services;
using Mediator;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.PeekIssuanceReportNumber;

public sealed class PeekIssuanceReportNumberQueryHandler(IIssuanceReportNumberGenerator reportNumbers)
    : IQueryHandler<PeekIssuanceReportNumberQuery, string>
{
    public async ValueTask<string> Handle(PeekIssuanceReportNumberQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await reportNumbers.PeekAsync(query.Type, query.Date, cancellationToken).ConfigureAwait(false);
    }
}
