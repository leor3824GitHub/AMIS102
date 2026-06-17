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

    /// <summary>The inspector the requester nominated to assess the returned items. Captured at request time;
    /// the person who actually performs the inspection is recorded separately in <see cref="InspectedBy"/>.</summary>
    public EmployeeRef AssignedInspector { get; private set; } = default!;
    public EmployeeRef? InspectedBy { get; private set; }
    public EmployeeRef? ReceivedBy { get; private set; }
    public string? Remarks { get; private set; }
    public string? InspectionRemarks { get; private set; }

    public string? RejectionReason { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset? InspectedOnUtc { get; private set; }
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
        EmployeeRef assignedInspector,
        string? remarks)
    {
        ArgumentNullException.ThrowIfNull(returnedBy);
        ArgumentNullException.ThrowIfNull(assignedInspector);
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
            AssignedInspector = assignedInspector,
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

    /// <summary>Replaces the nominated inspector while the request is still <see cref="ReturnedPropertyReceiptStatus.Pending"/>
    /// (i.e. before anyone has inspected it). Mirrors the IAR reassign-inspector capability.</summary>
    public void ReassignInspector(EmployeeRef newInspector)
    {
        ArgumentNullException.ThrowIfNull(newInspector);
        if (Status != ReturnedPropertyReceiptStatus.Pending)
            throw new InvalidOperationException(
                $"Only Pending return requests can have their inspector reassigned. Current status: {Status}.");

        AssignedInspector = newInspector;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Inspector assesses a Pending return, recording each item's condition. Moves the request to
    /// <see cref="ReturnedPropertyReceiptStatus.Inspected"/> so the custodian can accept it. No asset state changes here.</summary>
    public void Inspect(EmployeeRef inspectedBy, IReadOnlyDictionary<Guid, AssetCondition> conditionsByItemId, string? remarks)
    {
        ArgumentNullException.ThrowIfNull(inspectedBy);
        ArgumentNullException.ThrowIfNull(conditionsByItemId);
        if (Status != ReturnedPropertyReceiptStatus.Pending)
            throw new InvalidOperationException($"Only Pending return requests can be inspected. Current status: {Status}.");

        // The inspection may only be performed by the inspector the requester nominated.
        // The handler resolves the actor from the authenticated identity and translates this to a 403.
        if (inspectedBy.EmployeeId != AssignedInspector.EmployeeId)
            throw new UnauthorizedAccessException(
                $"Only the assigned inspector ({AssignedInspector.PrintedName}) may inspect this return.");

        foreach (var item in _items)
        {
            if (!conditionsByItemId.TryGetValue(item.Id, out var condition))
                throw new InvalidOperationException($"Inspection is missing a condition for item #{item.ItemNo} ({item.Snapshot.PropertyNo}).");
            item.SetInspectedCondition(condition);
        }

        InspectedBy = inspectedBy;
        InspectionRemarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
        Status = ReturnedPropertyReceiptStatus.Inspected;
        InspectedOnUtc = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = InspectedOnUtc;
    }

    /// <summary>Custodian accepts the inspected return. Assigns the official receipt number and the receiver.
    /// The asset/accountability state changes are performed by the handler, which owns those aggregates.</summary>
    public void Accept(string receiptNo, EmployeeRef receivedBy)
    {
        if (string.IsNullOrWhiteSpace(receiptNo))
            throw new InvalidOperationException("A receipt number is required to accept a return.");
        ArgumentNullException.ThrowIfNull(receivedBy);
        if (Status != ReturnedPropertyReceiptStatus.Inspected)
            throw new InvalidOperationException($"Only Inspected return requests can be accepted. Current status: {Status}.");

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
        if (Status is not (ReturnedPropertyReceiptStatus.Pending or ReturnedPropertyReceiptStatus.Inspected))
            throw new InvalidOperationException($"Only Pending or Inspected return requests can be rejected. Current status: {Status}.");

        Status = ReturnedPropertyReceiptStatus.Rejected;
        RejectionReason = reason.Trim();
        ResolvedOnUtc = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = ResolvedOnUtc;
    }

    public void Cancel(string? reason)
    {
        if (Status is not (ReturnedPropertyReceiptStatus.Pending or ReturnedPropertyReceiptStatus.Inspected))
            throw new InvalidOperationException($"Only Pending or Inspected return requests can be cancelled. Current status: {Status}.");

        Status = ReturnedPropertyReceiptStatus.Cancelled;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        ResolvedOnUtc = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = ResolvedOnUtc;
    }
}
