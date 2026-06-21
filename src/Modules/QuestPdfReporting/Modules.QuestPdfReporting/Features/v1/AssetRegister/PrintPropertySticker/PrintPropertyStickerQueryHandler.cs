using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using AMIS.Modules.AssetRegister.Contracts.v1.Locations;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.References;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPropertySticker;

public sealed class PrintPropertyStickerQueryHandler(IMediator mediator)
    : IQueryHandler<PrintPropertyStickerQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintPropertyStickerQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var propertyNo = (query.PropertyNo ?? string.Empty).Trim();

        var asset = await mediator.Send(new GetAssetByPropertyNoQuery(propertyNo), cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset '{query.PropertyNo}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        string? accountableOfficer = null;
        if (asset.CurrentCustodianId is { } custodianId)
        {
            var employee = await mediator.Send(new GetEmployeeReferenceByIdQuery(custodianId), cancellationToken).ConfigureAwait(false);
            if (employee is not null)
                accountableOfficer = $"{employee.FirstName} {employee.LastName}".Trim();
        }

        string? location = null;
        if (asset.CurrentLocationId is { } locationId)
        {
            var loc = await mediator.Send(new GetLocationReferenceByIdQuery(locationId), cancellationToken).ConfigureAwait(false);
            if (loc is not null)
                location = string.IsNullOrWhiteSpace(loc.Name) ? loc.Code : loc.Name;
        }

        var model = new PropertyStickerModel(
            asset.PropertyNo,
            asset.Description,
            asset.SerialNo,
            asset.AcquisitionDate,
            asset.UnitCost,
            asset.AssetType,
            accountableOfficer,
            location);

        return new PropertyStickerPdfDocument([model], org, query.PaperSize).GeneratePdf();
    }
}
