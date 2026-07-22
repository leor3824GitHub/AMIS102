using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Data.Services;
using Mediator;

namespace AMIS.Modules.AssetRegister.Features.v1.Transfers.ResolveTransferDestination;

/// <summary>
/// Backs the derived destination banner on the PPEIR create form. All the work lives in
/// <see cref="TransferDestinationResolver"/> so the create-issuance handler enforces the identical rule
/// server-side and the two can never disagree.
/// </summary>
public sealed class ResolveTransferDestinationQueryHandler(
    TransferDestinationResolver resolver,
    AssetRegisterDbContext db)
    : IQueryHandler<ResolveTransferDestinationQuery, TransferDestinationDto?>
{
    public async ValueTask<TransferDestinationDto?> Handle(
        ResolveTransferDestinationQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var currentTenantId = db.TenantInfo?.Identifier ?? string.Empty;

        return await resolver
            .ResolveForEmployeeAsync(query.EmployeeId, currentTenantId, cancellationToken)
            .ConfigureAwait(false);
    }
}
