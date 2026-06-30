using AMIS.Framework.Core.Context;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using AMIS.Modules.AssetRegister.Features.v1.Shared;
using Mediator;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.GetMyAccountableAssets;

public sealed class GetMyAccountableAssetsQueryHandler(ICurrentUser currentUser, IMediator mediator)
    : IQueryHandler<GetMyAccountableAssetsQuery, PagedResponse<AssetRegistrySummaryDto>>
{
    public async ValueTask<PagedResponse<AssetRegistrySummaryDto>> Handle(
        GetMyAccountableAssetsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var employee = await CurrentEmployeeResolver.TryResolveAsync(currentUser, mediator, cancellationToken).ConfigureAwait(false);
        if (employee is null)
        {
            return new PagedResponse<AssetRegistrySummaryDto>
            {
                Items = [],
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = 0,
                TotalPages = 0
            };
        }

        // Reuse the asset search, scoped server-side to assets whose current custodian is the
        // resolved employee — i.e. the units they are currently accountable for. The custodian is
        // set to the accountability's ReceivedBy at issue time (AssignTo), and cleared on
        // return/transfer-out, so this is exactly "what I hold right now" — the per-asset mirror of
        // the per-document My Accountability list.
        return await mediator.Send(new SearchAssetsQuery(
            Keyword: query.Keyword,
            SerialNo: null,
            AssetType: query.AssetType,
            LifecycleState: query.LifecycleState,
            CurrentCustodianId: employee.Id,
            IncludeTransferredOut: false,
            PageNumber: pageNumber,
            PageSize: pageSize,
            PropertyClass: null), cancellationToken).ConfigureAwait(false);
    }
}
