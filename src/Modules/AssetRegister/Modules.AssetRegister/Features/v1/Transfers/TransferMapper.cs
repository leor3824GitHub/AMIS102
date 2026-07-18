using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;
using AMIS.Modules.AssetRegister.Domain.Transfers;

namespace AMIS.Modules.AssetRegister.Features.v1.Transfers;

internal static class TransferMapper
{
    public static AssetTransferOfferLineDto ToDto(AssetTransferOfferLine l) =>
        new(l.Id, l.ItemNo, l.SourcePropertyNo, l.Description, l.SerialNo, l.Brand, l.Model,
            l.UnitCost, l.OriginalAcquisitionDate, l.AccumulatedDepreciation,
            l.DepreciationCurrentThrough, l.NetBookValue, l.CatalogUacsCode);

    public static AssetTransferOfferDto ToDto(AssetTransferOffer o) =>
        new(o.Id, o.CorrelationId, o.Direction, o.FromTenantId, o.FromAgencyName, o.ToTenantId, o.ToAgencyName,
            o.SourceIssuanceReportNo, o.IssuanceReportType, o.Status, o.ReceivingReportId, o.ReceivingReportNo,
            o.RejectedReason, o.RespondedUtc, o.CreatedOnUtc, o.TotalUnitCost, o.TotalNetBookValue,
            [.. o.Lines.OrderBy(l => l.ItemNo).Select(ToDto)]);
}
