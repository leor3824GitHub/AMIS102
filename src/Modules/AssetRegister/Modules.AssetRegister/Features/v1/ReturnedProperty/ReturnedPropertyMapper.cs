using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Contracts.v1.ReturnedProperty;
using AMIS.Modules.AssetRegister.Domain.ReturnedProperty;
using AMIS.Modules.AssetRegister.Features.v1.Accountability;

namespace AMIS.Modules.AssetRegister.Features.v1.ReturnedProperty;

internal static class ReturnedPropertyMapper
{
    public static ReturnedPropertyReceiptItemDto ToDto(ReturnedPropertyReceiptItem i) =>
        new(i.Id, i.ReceiptId, i.AccountabilityLineId, i.AssetRegistryId, i.ItemNo,
            AccountabilityMapper.ToDto(i.Snapshot),
            i.InspectedCondition);

    public static ReturnedPropertyReceiptDto ToDto(ReturnedPropertyReceipt r) =>
        new(r.Id, r.ReceiptNo, r.ReceiptType, r.Status, r.Date,
            r.AccountabilityId, r.AccountabilityDocumentNo,
            AccountabilityMapper.ToDto(r.ReturnedBy),
            r.ReceivedBy is null ? null : AccountabilityMapper.ToDto(r.ReceivedBy),
            r.Remarks,
            r.RejectionReason,
            r.CancellationReason,
            r.Items.OrderBy(i => i.ItemNo).Select(ToDto).ToList(),
            r.InspectedBy is null ? null : AccountabilityMapper.ToDto(r.InspectedBy),
            r.InspectionRemarks);

    public static ReturnedPropertyReceiptSummaryDto ToSummaryDto(ReturnedPropertyReceipt r) =>
        new(r.Id, r.ReceiptNo, r.ReceiptType, r.Status, r.Date,
            r.AccountabilityDocumentNo,
            r.Items.Count,
            r.Items.Sum(i => i.Snapshot.UnitCost));
}
