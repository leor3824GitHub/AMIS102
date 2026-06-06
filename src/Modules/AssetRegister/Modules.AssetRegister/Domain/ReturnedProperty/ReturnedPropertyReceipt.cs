using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;

namespace AMIS.Modules.AssetRegister.Domain.ReturnedProperty;

public sealed class ReturnedPropertyReceipt : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    /// <summary>Official receipt number (RRSP/RRP). Null while the request is Pending; assigned on acceptance.</summary>
    public string? ReceiptNo { get; private set; }
    public ReturnedPropertyReceiptType ReceiptType { get; private set; }
    public ReturnedPropertyReceiptStatus Status { get; private set; }
    public DateOnly Date { get; private set; }

    /// <summary>The ICS/PAR this receipt is against.</summary>
    public Guid AccountabilityId { get; private set; }

    /// <summary>Snapshot of the accountability document number at time of return.</summary>
    public string AccountabilityDocumentNo { get; private set; } = default!;

    public EmployeeRef ReturnedBy { get; private set; } = default!;
    public EmployeeRef? ReceivedBy { get; private set; }
    public string? Remarks { get; private set; }

    public string? RejectionReason { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset? AcceptedOnUtc { get; private set; }
    public DateTimeOffset? ResolvedOnUtc { get; private set; }

    private readonly List<ReturnedPropertyReceiptItem> _items = [];
    public IReadOnlyCollection<ReturnedPropertyReceiptItem> Items => _items.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private ReturnedPropertyReceipt() { }

    /// <summary>Raises a return request. The receipt starts <see cref="ReturnedPropertyReceiptStatus.Pending"/> with no
    /// official number and no receiver — both are set when a custodian accepts.</summary>
    public static ReturnedPropertyReceipt Create(
        string tenantId,
        ReturnedPropertyReceiptType receiptType,
        DateOnly date,
        Guid accountabilityId,
        string accountabilityDocumentNo,
        EmployeeRef returnedBy,
        string? remarks)
    {
        ArgumentNullException.ThrowIfNull(returnedBy);
        return new ReturnedPropertyReceipt
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReceiptNo = null,
            ReceiptType = receiptType,
            Status = ReturnedPropertyReceiptStatus.Pending,
            Date = date,
            AccountabilityId = accountabilityId,
            AccountabilityDocumentNo = accountabilityDocumentNo,
            ReturnedBy = returnedBy,
            ReceivedBy = null,
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

    /// <summary>Custodian accepts the return. Assigns the official receipt number and the receiver.
    /// The asset/accountability state changes are performed by the handler, which owns those aggregates.</summary>
    public void Accept(string receiptNo, EmployeeRef receivedBy)
    {
        if (string.IsNullOrWhiteSpace(receiptNo))
            throw new InvalidOperationException("A receipt number is required to accept a return.");
        ArgumentNullException.ThrowIfNull(receivedBy);
        if (Status != ReturnedPropertyReceiptStatus.Pending)
            throw new InvalidOperationException($"Only Pending return requests can be accepted. Current status: {Status}.");

        ReceiptNo = receiptNo;
        ReceivedBy = receivedBy;
        Status = ReturnedPropertyReceiptStatus.Accepted;
        AcceptedOnUtc = DateTimeOffset.UtcNow;
        ResolvedOnUtc = AcceptedOnUtc;
        LastModifiedOnUtc = AcceptedOnUtc;
    }

    public void Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("A reason is required to reject a return request.");
        if (Status != ReturnedPropertyReceiptStatus.Pending)
            throw new InvalidOperationException($"Only Pending return requests can be rejected. Current status: {Status}.");

        Status = ReturnedPropertyReceiptStatus.Rejected;
        RejectionReason = reason.Trim();
        ResolvedOnUtc = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = ResolvedOnUtc;
    }

    public void Cancel(string? reason)
    {
        if (Status != ReturnedPropertyReceiptStatus.Pending)
            throw new InvalidOperationException($"Only Pending return requests can be cancelled. Current status: {Status}.");

        Status = ReturnedPropertyReceiptStatus.Cancelled;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        ResolvedOnUtc = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = ResolvedOnUtc;
    }
}
