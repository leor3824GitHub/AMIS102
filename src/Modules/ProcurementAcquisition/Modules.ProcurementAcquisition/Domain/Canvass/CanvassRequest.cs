using FSH.Framework.Core.Domain;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.Canvass;

namespace FSH.Modules.ProcurementAcquisition.Domain.Canvass;

public sealed class CanvassRequest : AggregateRoot<Guid>, IAuditableEntity
{
    public string RivNumber { get; private set; } = default!;
    public Guid PurchaseRequestId { get; private set; }
    public DateOnly ReturnDeadline { get; private set; }
    public CanvassRequestStatus Status { get; private set; }
    public Guid? AwardedSupplierId { get; private set; }
    public byte[] Version { get; set; } = [];

    // Navigation
    public ICollection<CanvassQuotation> Quotations { get; private set; } = new List<CanvassQuotation>();

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    private CanvassRequest() { }

    public static CanvassRequest Create(string rivNumber, Guid purchaseRequestId, DateOnly returnDeadline)
    {
        return new CanvassRequest
        {
            Id = Guid.NewGuid(),
            RivNumber = rivNumber,
            PurchaseRequestId = purchaseRequestId,
            ReturnDeadline = returnDeadline,
            Status = CanvassRequestStatus.Open,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public void Award(Guid awardedSupplierId)
    {
        if (Status != CanvassRequestStatus.Open && Status != CanvassRequestStatus.Evaluated)
            throw new InvalidOperationException("Cannot award a canvass that is not Open or Evaluated.");

        Status = CanvassRequestStatus.Awarded;
        AwardedSupplierId = awardedSupplierId;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == CanvassRequestStatus.Awarded || Status == CanvassRequestStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel a canvass request with status '{Status}'.");

        Status = CanvassRequestStatus.Cancelled;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Evaluate()
    {
        if (Status != CanvassRequestStatus.Open)
            throw new InvalidOperationException("Only Open canvass requests can be set to Evaluated.");

        Status = CanvassRequestStatus.Evaluated;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
