using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;

namespace AMIS.Modules.AssetRegister.Domain.ReturnedProperty;

public sealed class ReturnedPropertyReceipt : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    public string ReceiptNo { get; private set; } = default!;
    public ReturnedPropertyReceiptType ReceiptType { get; private set; }
    public DateOnly Date { get; private set; }

    /// <summary>The ICS/PAR this receipt is against.</summary>
    public Guid AccountabilityId { get; private set; }

    /// <summary>Snapshot of the accountability document number at time of return.</summary>
    public string AccountabilityDocumentNo { get; private set; } = default!;

    public EmployeeRef ReturnedBy { get; private set; } = default!;
    public EmployeeRef? ReceivedBy { get; private set; }
    public string? Remarks { get; private set; }

    private readonly List<ReturnedPropertyReceiptItem> _items = [];
    public IReadOnlyCollection<ReturnedPropertyReceiptItem> Items => _items.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private ReturnedPropertyReceipt() { }

    public static ReturnedPropertyReceipt Create(
        string tenantId,
        string receiptNo,
        ReturnedPropertyReceiptType receiptType,
        DateOnly date,
        Guid accountabilityId,
        string accountabilityDocumentNo,
        EmployeeRef returnedBy,
        EmployeeRef? receivedBy,
        string? remarks)
    {
        ArgumentNullException.ThrowIfNull(returnedBy);
        return new ReturnedPropertyReceipt
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReceiptNo = receiptNo,
            ReceiptType = receiptType,
            Date = date,
            AccountabilityId = accountabilityId,
            AccountabilityDocumentNo = accountabilityDocumentNo,
            ReturnedBy = returnedBy,
            ReceivedBy = receivedBy,
            Remarks = remarks,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public void AddItem(
        Guid accountabilityLineId,
        Guid assetRegistryId,
        int itemNo,
        AssetSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _items.Add(ReturnedPropertyReceiptItem.Create(TenantId, Id, accountabilityLineId, assetRegistryId, itemNo, snapshot));
    }
}
