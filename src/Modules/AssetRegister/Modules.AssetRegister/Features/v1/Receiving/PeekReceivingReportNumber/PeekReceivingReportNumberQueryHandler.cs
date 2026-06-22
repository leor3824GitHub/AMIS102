using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using AMIS.Modules.AssetRegister.Domain.Services;
using Mediator;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.PeekReceivingReportNumber;

public sealed class PeekReceivingReportNumberQueryHandler(IReceivingReportNumberGenerator reportNumbers)
    : IQueryHandler<PeekReceivingReportNumberQuery, string>
{
    public async ValueTask<string> Handle(PeekReceivingReportNumberQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await reportNumbers.PeekAsync(query.Kind, query.Date, cancellationToken).ConfigureAwait(false);
    }
}
