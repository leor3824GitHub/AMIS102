using AMIS.Framework.Core.Context;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Features.v1.Shared;
using Mediator;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.GetMyAccountabilities;

public sealed class GetMyAccountabilitiesQueryHandler(ICurrentUser currentUser, IMediator mediator)
    : IQueryHandler<GetMyAccountabilitiesQuery, PagedResponse<PropertyAccountabilitySummaryDto>>
{
    public async ValueTask<PagedResponse<PropertyAccountabilitySummaryDto>> Handle(
        GetMyAccountabilitiesQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var employee = await CurrentEmployeeResolver.TryResolveAsync(currentUser, mediator, ct).ConfigureAwait(false);
        if (employee is null)
        {
            return new PagedResponse<PropertyAccountabilitySummaryDto>
            {
                Items = [],
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = 0,
                TotalPages = 0
            };
        }

        // Reuse the officer search, scoped server-side to the resolved employee.
        return await mediator.Send(new SearchAccountabilitiesQuery(
            query.Keyword, query.Type, query.Status, employee.Id, null, null, pageNumber, pageSize), ct)
            .ConfigureAwait(false);
    }
}
