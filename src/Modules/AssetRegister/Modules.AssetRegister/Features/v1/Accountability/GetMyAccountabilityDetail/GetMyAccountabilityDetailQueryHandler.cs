using AMIS.Framework.Core.Context;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Features.v1.Shared;
using Mediator;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.GetMyAccountabilityDetail;

public sealed class GetMyAccountabilityDetailQueryHandler(ICurrentUser currentUser, IMediator mediator)
    : IQueryHandler<GetMyAccountabilityDetailQuery, PropertyAccountabilityDto?>
{
    public async ValueTask<PropertyAccountabilityDto?> Handle(GetMyAccountabilityDetailQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var employee = await CurrentEmployeeResolver.TryResolveAsync(currentUser, mediator, ct).ConfigureAwait(false);
        if (employee is null)
            return null;

        var dto = await mediator.Send(new GetAccountabilityQuery(query.Id), ct).ConfigureAwait(false);

        // Ownership gate: only the named recipient may view the document — even by direct id.
        if (dto is null || dto.ReceivedBy.EmployeeId != employee.Id)
            return null;

        return dto;
    }
}
