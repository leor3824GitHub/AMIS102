using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.Multitenancy.Contracts;
using Mediator;

namespace AMIS.Modules.AssetRegister.Features.v1.Transfers.GetTransferDestinations;

/// <summary>
/// Backs the destination-agency picker on the PPEIR create form. Returns active tenants other than the
/// current one, projected down to identifier + display name — no connection strings, admin emails, or
/// subscription data ever leave the tenant registry through this endpoint.
/// </summary>
public sealed class GetTransferDestinationsQueryHandler(
    ITenantService tenantService,
    AssetRegisterDbContext db)
    : IQueryHandler<GetTransferDestinationsQuery, IReadOnlyList<TransferDestinationDto>>
{
    public async ValueTask<IReadOnlyList<TransferDestinationDto>> Handle(
        GetTransferDestinationsQuery query, CancellationToken cancellationToken)
    {
        var currentTenantId = db.TenantInfo?.Identifier ?? string.Empty;

        var tenants = await tenantService.GetAllTenantInfosAsync(cancellationToken).ConfigureAwait(false);

        return
        [
            .. tenants
                .Where(t => t.IsActive
                         && !string.Equals(t.Identifier, currentTenantId, StringComparison.OrdinalIgnoreCase))
                .Select(t => new TransferDestinationDto(t.Identifier, t.Name ?? t.Identifier))
                .OrderBy(d => d.AgencyName, StringComparer.OrdinalIgnoreCase)
        ];
    }
}
