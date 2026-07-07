using AMIS.Modules.AssetRegister.Data.Services;
using Mediator;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.GetAssetImage;

/// <summary>Which stored variant to serve — the small list thumbnail or the full image.</summary>
public enum AssetImageVariant
{
    Full = 0,
    Thumbnail = 1
}

/// <summary>
/// Fetches a single asset's photo as raw bytes for the lazy image endpoint. REST-only (no other
/// module sends this via Mediator), so the query lives in the module, not Contracts. The result type
/// <see cref="AssetImageResult"/> is owned by <see cref="AssetImageStorage"/>.
/// </summary>
public sealed record GetAssetImageQuery(Guid Id, AssetImageVariant Variant = AssetImageVariant.Full)
    : IQuery<AssetImageResult?>;
