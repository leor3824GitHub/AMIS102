using System.Net;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using Mediator;

namespace AMIS.Modules.Vehicle.Features.v1.MyVehicle;

/// <summary>Shared accepted-PAR ownership gate for accountable-officer self-service.</summary>
internal static class MyVehicleGuard
{
    /// <summary>
    /// Throws <see cref="CustomException"/> (403) if the given asset is not on one of the current
    /// user's accepted PARs.
    /// </summary>
    public static async ValueTask EnsureOwnedAsync(
        IMediator mediator, Guid assetRegistryId, CancellationToken cancellationToken)
    {
        var assetIds = await mediator.Send(new GetMyAccountableAssetIdsQuery(AssetType.PPE), cancellationToken).ConfigureAwait(false);
        if (!assetIds.Contains(assetRegistryId))
            throw new CustomException("This vehicle is not assigned to you.", Array.Empty<string>(), HttpStatusCode.Forbidden);
    }
}
