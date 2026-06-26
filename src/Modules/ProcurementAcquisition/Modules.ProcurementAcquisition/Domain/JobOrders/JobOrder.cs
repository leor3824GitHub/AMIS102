using AMIS.Framework.Core.Domain;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;

namespace AMIS.Modules.ProcurementAcquisition.Domain.JobOrders;

/// <summary>Domain-internal carrier for JO line item input. Mirrors <c>PurchaseOrderLineItemData</c>, minus
/// the StockNumber/CatalogItemId references — works (renovation/repair/fabrication) are not inventoried.</summary>
public readonly record struct JobOrderLineItemData(
    string? Unit,
    string Description,
    decimal Quantity,
    decimal UnitCost);

public sealed class JobOrderLineItem
{
    public int ItemNo { get; private set; }
    public string? Unit { get; private set; }
    public string Description { get; private set; } = default!;
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal Amount => Quantity * UnitCost;

    private JobOrderLineItem() { }

    public static JobOrderLineItem Create(
        int itemNo,
        string? unit,
        string description,
        decimal quantity,
        decimal unitCost)
    {
        return new JobOrderLineItem
        {
            ItemNo = itemNo,
            Unit = unit,
            Description = description,
            Quantity = quantity,
            UnitCost = unitCost
        };
    }
}

/// <summary>
/// A Job Order for works — renovation, repair, or fabrication. Runs the same approval workflow as a Purchase
/// Order (Submit → Funds Available → Issue) but, because works are not inventoried, captures inspection and
/// acceptance on the JO itself rather than feeding an Inspection &amp; Acceptance Report. A JO may originate
/// from a Purchase Request (<see cref="PurchaseRequestId"/> set) or stand alone.
/// </summary>
public sealed class JobOrder : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public string JoNumber { get; private set; } = default!;
    public DateOnly JoDate { get; private set; }

    /// <summary>Optional link to a source Purchase Request when the JO is request-driven; null when standalone.</summary>
    public Guid? PurchaseRequestId { get; private set; }

    /// <summary>Free-text "Job Request No." printed on the form (e.g. the source request reference).</summary>
    public string? JobRequestNo { get; private set; }

    /// <summary>"Requisitioning Office/Dept." printed on the form.</summary>
    public string? RequisitioningOffice { get; private set; }

    public Guid SupplierId { get; private set; }
    public string SupplierName { get; private set; } = default!;
    public string SupplierAddress { get; private set; } = default!;
    public string? SupplierTin { get; private set; }
    public ModeOfProcurement ModeOfProcurement { get; private set; }
    public string PlaceOfDelivery { get; private set; } = default!;
    public DateOnly? DateOfDelivery { get; private set; }
    public string DeliveryTerm { get; private set; } = default!;
    public string PaymentTerm { get; private set; } = default!;
    public string? FundCluster { get; private set; }
    public string? OursBursNumber { get; private set; }
    public DateOnly? OursBursDate { get; private set; }
    public JobOrderStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }

    // "Funds Available" — Accountant signs and captures the BUR/ORS
    public Guid? FundsAvailableCertifiedById { get; private set; }
    public string? FundsAvailableCertifiedByName { get; private set; }
    public string? FundsAvailableCertifiedByDesignation { get; private set; }
    public DateTimeOffset? FundsAvailableCertifiedOnUtc { get; private set; }

    // "Approved" — Authorized Official who issued the JO (printed in the bottom-right signature block)
    public Guid? IssuedById { get; private set; }
    public string? IssuedByName { get; private set; }
    public string? IssuedByDesignation { get; private set; }
    public DateTimeOffset? IssuedOnUtc { get; private set; }

    // Inspection — C.O./F.O. Inspector (self-contained, no IAR)
    public Guid? InspectedById { get; private set; }
    public string? InspectedByName { get; private set; }
    public string? InspectedByDesignation { get; private set; }
    public DateTimeOffset? InspectedOnUtc { get; private set; }
    public string? InspectionInvoiceNo { get; private set; }
    public DateOnly? InspectionInvoiceDate { get; private set; }
    public DateOnly? DateInspected { get; private set; }
    public string? InspectionFindings { get; private set; }
    public bool FoundInOrder { get; private set; }

    // Acceptance — PMSDS-GSD / F.O. Supply Officer (self-contained, no IAR)
    public Guid? AcceptedById { get; private set; }
    public string? AcceptedByName { get; private set; }
    public string? AcceptedByDesignation { get; private set; }
    public DateTimeOffset? AcceptedOnUtc { get; private set; }
    public string? AcceptanceInvoiceNo { get; private set; }
    public DateOnly? DateReceived { get; private set; }
    public bool IsCompleteDelivery { get; private set; } = true;
    public string? PartialDeliveryNote { get; private set; }

    private readonly List<JobOrderLineItem> _lineItems = [];
    public IReadOnlyList<JobOrderLineItem> LineItems => _lineItems.AsReadOnly();

    public decimal TotalAmount => _lineItems.Sum(x => x.Amount);

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    private JobOrder() { }

    public static JobOrder Create(
        string tenantId,
        string joNumber,
        Guid? purchaseRequestId,
        string? jobRequestNo,
        string? requisitioningOffice,
        Guid supplierId,
        string supplierName,
        string supplierAddress,
        string? supplierTin,
        ModeOfProcurement modeOfProcurement,
        string placeOfDelivery,
        DateOnly? dateOfDelivery,
        string deliveryTerm,
        string paymentTerm,
        string? fundCluster,
        string? oursBursNumber,
        IEnumerable<JobOrderLineItemData> lineItems)
    {
        var jo = new JobOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            JoNumber = joNumber,
            JoDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PurchaseRequestId = purchaseRequestId,
            JobRequestNo = jobRequestNo,
            RequisitioningOffice = requisitioningOffice,
            SupplierId = supplierId,
            SupplierName = supplierName,
            SupplierAddress = supplierAddress,
            SupplierTin = supplierTin,
            ModeOfProcurement = modeOfProcurement,
            PlaceOfDelivery = placeOfDelivery,
            DateOfDelivery = dateOfDelivery,
            DeliveryTerm = deliveryTerm,
            PaymentTerm = paymentTerm,
            FundCluster = fundCluster,
            OursBursNumber = oursBursNumber,
            OursBursDate = null,
            Status = JobOrderStatus.Draft,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

        var itemNo = 1;
        foreach (var li in lineItems)
        {
            jo._lineItems.Add(JobOrderLineItem.Create(
                itemNo++, li.Unit, li.Description, li.Quantity, li.UnitCost));
        }

        return jo;
    }

    public void Update(
        string? jobRequestNo,
        string? requisitioningOffice,
        Guid supplierId,
        string supplierName,
        string supplierAddress,
        string? supplierTin,
        ModeOfProcurement modeOfProcurement,
        string placeOfDelivery,
        DateOnly? dateOfDelivery,
        string deliveryTerm,
        string paymentTerm,
        string? fundCluster,
        string? oursBursNumber,
        IEnumerable<JobOrderLineItemData> lineItems)
    {
        if (Status != JobOrderStatus.Draft)
            throw new InvalidOperationException("Only Draft job orders can be updated.");

        JobRequestNo = jobRequestNo;
        RequisitioningOffice = requisitioningOffice;
        SupplierId = supplierId;
        SupplierName = supplierName;
        SupplierAddress = supplierAddress;
        SupplierTin = supplierTin;
        ModeOfProcurement = modeOfProcurement;
        PlaceOfDelivery = placeOfDelivery;
        DateOfDelivery = dateOfDelivery;
        DeliveryTerm = deliveryTerm;
        PaymentTerm = paymentTerm;
        FundCluster = fundCluster;
        OursBursNumber = oursBursNumber;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;

        _lineItems.Clear();
        var itemNo = 1;
        foreach (var li in lineItems)
        {
            _lineItems.Add(JobOrderLineItem.Create(
                itemNo++, li.Unit, li.Description, li.Quantity, li.UnitCost));
        }
    }

    /// <summary>Buyer submits the JO. Moves Draft → PendingFundsAvailable (awaiting Accountant).</summary>
    public void Submit()
    {
        if (Status != JobOrderStatus.Draft)
            throw new InvalidOperationException("Only Draft job orders can be submitted.");
        if (_lineItems.Count == 0)
            throw new InvalidOperationException("Job order must have at least one line item.");

        Status = JobOrderStatus.PendingFundsAvailable;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Accountant signs "Funds Available" and optionally captures the BUR/ORS reference and Fund Cluster.
    /// Moves PendingFundsAvailable → PendingApproval (awaiting Issue).
    /// </summary>
    public void CertifyFundsAvailable(
        Guid certifiedById,
        string certifiedByName,
        string? oursBursNumber,
        DateOnly? oursBursDate,
        string? fundCluster,
        string? certifiedByDesignation = null)
    {
        if (Status != JobOrderStatus.PendingFundsAvailable)
            throw new InvalidOperationException("Funds Available can only be certified on job orders awaiting Accountant review.");
        if (string.IsNullOrWhiteSpace(certifiedByName))
            throw new InvalidOperationException("Accountant name is required.");

        if (!string.IsNullOrWhiteSpace(oursBursNumber))
        {
            OursBursNumber = oursBursNumber;
            OursBursDate = oursBursDate;
        }

        if (!string.IsNullOrWhiteSpace(fundCluster))
            FundCluster = fundCluster;

        FundsAvailableCertifiedById = certifiedById;
        FundsAvailableCertifiedByName = certifiedByName;
        FundsAvailableCertifiedByDesignation = certifiedByDesignation;
        FundsAvailableCertifiedOnUtc = DateTimeOffset.UtcNow;

        Status = JobOrderStatus.PendingApproval;
        LastModifiedOnUtc = FundsAvailableCertifiedOnUtc;
    }

    /// <summary>
    /// Authorized Official approves and issues the JO. Captures who issued it for the "Very truly yours"
    /// signature block on the printed JO. Moves PendingApproval → Issued.
    /// </summary>
    public void Issue(Guid issuedById, string? issuedByName, string? issuedByDesignation = null)
    {
        if (Status != JobOrderStatus.PendingApproval)
            throw new InvalidOperationException("Only job orders that have passed Funds Available certification can be issued.");
        if (_lineItems.Count == 0)
            throw new InvalidOperationException("Job order must have at least one line item.");

        IssuedById = issuedById;
        IssuedByName = issuedByName;
        IssuedByDesignation = issuedByDesignation;
        IssuedOnUtc = DateTimeOffset.UtcNow;

        Status = JobOrderStatus.Issued;
        LastModifiedOnUtc = IssuedOnUtc.Value;
    }

    /// <summary>
    /// C.O./F.O. Inspector records the inspection result on an Issued JO. The signatory name/designation are
    /// resolved from the authenticated identity by the handler. Moves Issued → Inspected.
    /// </summary>
    public void Inspect(
        Guid inspectedById,
        string? inspectedByName,
        string? inspectedByDesignation,
        string? invoiceNo,
        DateOnly? invoiceDate,
        DateOnly? dateInspected,
        string? findings,
        bool foundInOrder)
    {
        if (Status != JobOrderStatus.Issued)
            throw new InvalidOperationException("Only Issued job orders can be inspected.");

        InspectedById = inspectedById;
        InspectedByName = inspectedByName;
        InspectedByDesignation = inspectedByDesignation;
        InspectedOnUtc = DateTimeOffset.UtcNow;
        InspectionInvoiceNo = invoiceNo;
        InspectionInvoiceDate = invoiceDate;
        DateInspected = dateInspected ?? DateOnly.FromDateTime(DateTime.UtcNow);
        InspectionFindings = findings;
        FoundInOrder = foundInOrder;

        Status = JobOrderStatus.Inspected;
        LastModifiedOnUtc = InspectedOnUtc;
    }

    /// <summary>
    /// Supply Officer records acceptance of the completed work on an Inspected JO. The signatory
    /// name/designation are resolved from the authenticated identity by the handler. Moves Inspected → Completed.
    /// </summary>
    public void Accept(
        Guid acceptedById,
        string? acceptedByName,
        string? acceptedByDesignation,
        string? invoiceNo,
        DateOnly? dateReceived,
        bool isCompleteDelivery,
        string? partialDeliveryNote)
    {
        if (Status != JobOrderStatus.Inspected)
            throw new InvalidOperationException("Only Inspected job orders can be accepted.");

        AcceptedById = acceptedById;
        AcceptedByName = acceptedByName;
        AcceptedByDesignation = acceptedByDesignation;
        AcceptedOnUtc = DateTimeOffset.UtcNow;
        AcceptanceInvoiceNo = invoiceNo;
        DateReceived = dateReceived ?? DateOnly.FromDateTime(DateTime.UtcNow);
        IsCompleteDelivery = isCompleteDelivery;
        PartialDeliveryNote = isCompleteDelivery ? null : partialDeliveryNote;

        Status = JobOrderStatus.Completed;
        LastModifiedOnUtc = AcceptedOnUtc;
    }

    public void Cancel(string? reason = null)
    {
        if (Status == JobOrderStatus.Completed || Status == JobOrderStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel a job order with status '{Status}'.");

        Status = JobOrderStatus.Cancelled;
        CancellationReason = reason;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
