using System.Security.Cryptography;
using FSH.Framework.Core.Domain;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;

namespace FSH.Modules.ProcurementPlanning.Domain.Ppmps;

public sealed class PpmpItem
{
    public Guid Id { get; private set; }
    public Guid PpmpId { get; private set; }
    public int ItemNo { get; private set; }
    public string GeneralDescription { get; private set; } = default!;
    public ProjectType ProjectType { get; private set; }
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = default!;
    public ModeOfProcurement ModeOfProcurement { get; private set; }
    public bool PreProcurementConference { get; private set; }
    public string ProcurementStart { get; private set; } = default!;
    public string ProcurementEnd { get; private set; } = default!;
    public string ExpectedDelivery { get; private set; } = default!;
    public string SourceOfFunds { get; private set; } = default!;
    public decimal EstimatedBudget { get; private set; }
    public string? SupportingDocuments { get; private set; }
    public string? Remarks { get; private set; }

    private PpmpItem() { }

    public static PpmpItem Create(
        Guid ppmpId,
        int itemNo,
        string generalDescription,
        ProjectType projectType,
        decimal quantity,
        string unit,
        ModeOfProcurement modeOfProcurement,
        bool preProcurementConference,
        string procurementStart,
        string procurementEnd,
        string expectedDelivery,
        string sourceOfFunds,
        decimal estimatedBudget,
        string? supportingDocuments,
        string? remarks) =>
        new()
        {
            Id = Guid.NewGuid(),
            PpmpId = ppmpId,
            ItemNo = itemNo,
            GeneralDescription = generalDescription,
            ProjectType = projectType,
            Quantity = quantity,
            Unit = unit,
            ModeOfProcurement = modeOfProcurement,
            PreProcurementConference = preProcurementConference,
            ProcurementStart = procurementStart,
            ProcurementEnd = procurementEnd,
            ExpectedDelivery = expectedDelivery,
            SourceOfFunds = sourceOfFunds,
            EstimatedBudget = estimatedBudget,
            SupportingDocuments = supportingDocuments,
            Remarks = remarks
        };
}

public sealed class Ppmp : AggregateRoot<Guid>, IAuditableEntity, ISoftDeletable
{
    public string PpmpNumber { get; private set; } = default!;
    public int FiscalYear { get; private set; }
    public PpmpType PpmpType { get; private set; }
    public string OfficeCode { get; private set; } = default!;
    public string EndUserUnit { get; private set; } = default!;
    public PpmpStatus Status { get; private set; }

    // Versioning
    public int VersionNumber { get; private set; }
    public bool IsCurrentVersion { get; private set; }
    public Guid VersionChainId { get; private set; }
    public Guid? PreviousVersionId { get; private set; }
    public string? AmendmentReason { get; private set; }
    public DateTimeOffset? AmendedAt { get; private set; }
    public string? AmendedById { get; private set; }

    public Guid PreparedById { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public Guid? ApprovedById { get; private set; }

    public string? ReturnReason { get; private set; }
    public DateTimeOffset? ReturnedAt { get; private set; }
    public Guid? ReturnedById { get; private set; }

    // Set when this PPMP is consolidated into an APP
    public Guid? AppId { get; private set; }

    public byte[] Version { get; set; } = [];

    private readonly List<PpmpItem> _items = [];
    public IReadOnlyList<PpmpItem> Items => _items.AsReadOnly();

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    private Ppmp() { }

    private static byte[] NewVersion() => RandomNumberGenerator.GetBytes(8);

    public static Ppmp Create(
        string ppmpNumber,
        int fiscalYear,
        PpmpType ppmpType,
        string officeCode,
        string endUserUnit,
        Guid preparedById,
        IEnumerable<PpmpItemRequest> items)
    {
        var chainId = Guid.NewGuid();
        var ppmp = new Ppmp
        {
            Id = Guid.NewGuid(),
            PpmpNumber = ppmpNumber,
            FiscalYear = fiscalYear,
            PpmpType = ppmpType,
            OfficeCode = officeCode,
            EndUserUnit = endUserUnit,
            PreparedById = preparedById,
            Status = PpmpStatus.Draft,
            VersionNumber = 1,
            IsCurrentVersion = true,
            VersionChainId = chainId,
            PreviousVersionId = null,
            CreatedOnUtc = DateTimeOffset.UtcNow,
            Version = NewVersion()
        };

        ppmp.ReplaceItems(items);
        return ppmp;
    }

    public void Update(
        int fiscalYear,
        PpmpType ppmpType,
        string officeCode,
        string endUserUnit,
        Guid preparedById,
        IEnumerable<PpmpItemRequest> items)
    {
        if (Status is not (PpmpStatus.Draft or PpmpStatus.Returned))
            throw new InvalidOperationException("Only Draft or Returned PPMPs can be updated.");

        FiscalYear = fiscalYear;
        PpmpType = ppmpType;
        OfficeCode = officeCode;
        EndUserUnit = endUserUnit;
        PreparedById = preparedById;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();

        ReplaceItems(items);
    }

    public void Submit()
    {
        if (Status is not (PpmpStatus.Draft or PpmpStatus.Returned))
            throw new InvalidOperationException("Only Draft or Returned PPMPs can be submitted.");
        if (_items.Count == 0)
            throw new InvalidOperationException("PPMP must have at least one item before submitting.");

        Status = PpmpStatus.Submitted;
        SubmittedAt = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    public void Approve(Guid approvedById)
    {
        if (Status != PpmpStatus.Submitted)
            throw new InvalidOperationException("Only Submitted PPMPs can be approved.");

        Status = PpmpStatus.Approved;
        ApprovedById = approvedById;
        ApprovedAt = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    public void Recall()
    {
        if (Status != PpmpStatus.Submitted)
            throw new InvalidOperationException("Only Submitted PPMPs can be recalled.");

        Status = PpmpStatus.Draft;
        SubmittedAt = null;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    public void Return(string returnReason, Guid returnedById)
    {
        if (Status != PpmpStatus.Submitted)
            throw new InvalidOperationException("Only Submitted PPMPs can be returned for revision.");

        Status = PpmpStatus.Returned;
        ReturnReason = returnReason;
        ReturnedAt = DateTimeOffset.UtcNow;
        ReturnedById = returnedById;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    public void MarkConsolidated(Guid appId)
    {
        if (Status != PpmpStatus.Approved)
            throw new InvalidOperationException("Only Approved PPMPs can be consolidated into an APP.");

        Status = PpmpStatus.Consolidated;
        AppId = appId;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    public void UnmarkConsolidated()
    {
        if (Status != PpmpStatus.Consolidated)
            throw new InvalidOperationException("Only Consolidated PPMPs can be unmarked.");

        Status = PpmpStatus.Approved;
        AppId = null;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    /// <summary>Creates a new version of this PPMP (amendment). Caller must set IsCurrentVersion=false on this instance.</summary>
    public Ppmp CreateAmendment(string amendmentReason, string amendedById)
    {
        if (Status is not (PpmpStatus.Approved or PpmpStatus.Consolidated))
            throw new InvalidOperationException("Only Approved or Consolidated PPMPs can be amended.");

        var amendment = new Ppmp
        {
            Id = Guid.NewGuid(),
            PpmpNumber = PpmpNumber,
            FiscalYear = FiscalYear,
            PpmpType = PpmpType,
            OfficeCode = OfficeCode,
            EndUserUnit = EndUserUnit,
            PreparedById = PreparedById,
            Status = PpmpStatus.Draft,
            VersionNumber = VersionNumber + 1,
            IsCurrentVersion = true,
            VersionChainId = VersionChainId,
            PreviousVersionId = Id,
            AmendmentReason = amendmentReason,
            AmendedAt = DateTimeOffset.UtcNow,
            AmendedById = amendedById,
            CreatedOnUtc = DateTimeOffset.UtcNow,
            Version = NewVersion()
        };

        // Clone items
        var itemNo = 1;
        foreach (var item in _items)
        {
            amendment._items.Add(PpmpItem.Create(
                amendment.Id, itemNo++,
                item.GeneralDescription, item.ProjectType,
                item.Quantity, item.Unit, item.ModeOfProcurement,
                item.PreProcurementConference, item.ProcurementStart,
                item.ProcurementEnd, item.ExpectedDelivery,
                item.SourceOfFunds, item.EstimatedBudget,
                item.SupportingDocuments, item.Remarks));
        }

        return amendment;
    }

    public void Supersede()
    {
        IsCurrentVersion = false;
        Status = PpmpStatus.Superseded;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    private void ReplaceItems(IEnumerable<PpmpItemRequest> items)
    {
        _items.Clear();
        var itemNo = 1;
        foreach (var r in items)
        {
            _items.Add(PpmpItem.Create(
                Id, itemNo++,
                r.GeneralDescription, r.ProjectType,
                r.Quantity, r.Unit, r.ModeOfProcurement,
                r.PreProcurementConference, r.ProcurementStart,
                r.ProcurementEnd, r.ExpectedDelivery,
                r.SourceOfFunds, r.EstimatedBudget,
                r.SupportingDocuments, r.Remarks));
        }
    }
}
