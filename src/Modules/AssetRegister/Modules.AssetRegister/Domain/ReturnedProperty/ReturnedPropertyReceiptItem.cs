using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;

namespace AMIS.Modules.AssetRegister.Domain.ReturnedProperty;

public sealed class ReturnedPropertyReceiptItem : IHasTenant
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = default!;
    public Guid ReceiptId { get; private set; }
    public Guid AccountabilityLineId { get; private set; }
    public Guid AssetRegistryId { get; private set; }
    public int ItemNo { get; private set; }
    public AssetSnapshot Snapshot { get; private set; } = default!;

    /// <summary>The condition the inspector assessed this item at. Null until the receipt is inspected.</summary>
    public AssetCondition? InspectedCondition { get; private set; }

    private ReturnedPropertyReceiptItem() { }

    /// <summary>Records the inspector's condition assessment for this returned item.</summary>
    internal void SetInspectedCondition(AssetCondition condition) => InspectedCondition = condition;

    internal static ReturnedPropertyReceiptItem Create(
        string tenantId,
        Guid receiptId,
        Guid accountabilityLineId,
        Guid assetRegistryId,
        int itemNo,
        AssetSnapshot snapshot)
    {
        return new ReturnedPropertyReceiptItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReceiptId = receiptId,
            AccountabilityLineId = accountabilityLineId,
            AssetRegistryId = assetRegistryId,
            ItemNo = itemNo,
            Snapshot = snapshot
        };
    }
}
