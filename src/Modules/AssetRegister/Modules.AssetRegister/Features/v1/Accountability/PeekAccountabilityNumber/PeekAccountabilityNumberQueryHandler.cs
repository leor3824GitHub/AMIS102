using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Domain.Services;
using Mediator;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.PeekAccountabilityNumber;

public sealed class PeekAccountabilityNumberQueryHandler(IAccountabilityNumberGenerator numbers)
    : IQueryHandler<PeekAccountabilityNumberQuery, string>
{
    public async ValueTask<string> Handle(PeekAccountabilityNumberQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Type != AccountabilityType.SE_ICS)
            return await numbers.PeekParAsync(query.Date, cancellationToken).ConfigureAwait(false);

        var category = query.HighValued ? AssetCategory.HighValuedSemi : AssetCategory.LowValuedSemi;
        return await numbers.PeekIcsAsync(category, query.Date, cancellationToken).ConfigureAwait(false);
    }
}
