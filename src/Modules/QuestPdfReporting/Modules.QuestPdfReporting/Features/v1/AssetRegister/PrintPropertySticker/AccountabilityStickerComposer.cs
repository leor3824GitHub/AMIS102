using System.Net;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using AMIS.Modules.AssetRegister.Contracts.v1.Locations;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPropertySticker;

/// <summary>
/// Shared builder behind the ICS and PAR sticker endpoints: loads an accountability document,
/// enforces it is the expected type, and renders one property sticker per line (each with a
/// Property-No QR code). The accountable officer is the document's <c>ReceivedBy</c>; the property
/// custodian comes from the Organization Profile.
/// </summary>
internal static class AccountabilityStickerComposer
{
    public static async ValueTask<byte[]> BuildAsync(
        IMediator          mediator,
        Guid               accountabilityId,
        AccountabilityType expectedType,
        string             paperSize,
        CancellationToken  cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mediator);

        var accountability = await mediator.Send(new GetAccountabilityQuery(accountabilityId), cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Accountability '{accountabilityId}' not found.");

        if (accountability.AccountabilityType != expectedType)
            throw new CustomException(
                $"Document '{accountability.DocumentNo}' is {Label(accountability.AccountabilityType)}, not {Label(expectedType)}.",
                Array.Empty<string>(),
                HttpStatusCode.BadRequest);

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        // The accountable person on an ICS/PAR is the receiving (ReceivedBy) employee.
        var accountableOfficer = accountability.ReceivedBy.PrintedName;

        // Location is not snapshotted on the ICS/PAR line — it lives on the asset's current location.
        // Resolve per asset, caching by location id (assets on one document usually share a location).
        var locationByLocationId = new Dictionary<Guid, string?>();
        var models = new List<PropertyStickerModel>(accountability.Lines.Count);

        foreach (var line in accountability.Lines)
        {
            var location = await ResolveLocationAsync(mediator, line.AssetRegistryId, locationByLocationId, cancellationToken).ConfigureAwait(false);
            var snapshot = line.Snapshot;

            models.Add(new PropertyStickerModel(
                snapshot.PropertyNo,
                snapshot.Description,
                snapshot.SerialNo,
                snapshot.AcquisitionDate,
                snapshot.UnitCost,
                snapshot.AssetType,
                accountableOfficer,
                location));
        }

        return new PropertyStickerPdfDocument(models, org, paperSize).GeneratePdf();
    }

    private static string Label(AccountabilityType type) =>
        type == AccountabilityType.SE_ICS ? "an ICS" : "a PAR";

    private static async ValueTask<string?> ResolveLocationAsync(
        IMediator mediator, Guid assetRegistryId, Dictionary<Guid, string?> cache, CancellationToken cancellationToken)
    {
        var asset = await mediator.Send(new GetAssetRegistryQuery(assetRegistryId), cancellationToken).ConfigureAwait(false);
        if (asset?.CurrentLocationId is not { } locationId)
            return null;

        if (cache.TryGetValue(locationId, out var cached))
            return cached;

        var loc = await mediator.Send(new GetLocationReferenceByIdQuery(locationId), cancellationToken).ConfigureAwait(false);
        string? name = null;
        if (loc is not null)
            name = string.IsNullOrWhiteSpace(loc.Name) ? loc.Code : loc.Name;
        cache[locationId] = name;
        return name;
    }
}
