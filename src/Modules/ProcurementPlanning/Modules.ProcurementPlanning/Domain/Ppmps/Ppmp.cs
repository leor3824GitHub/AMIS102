using System.Security.Cryptography;
using FSH.Framework.Core.Domain;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;

namespace FSH.Modules.ProcurementPlanning.Domain.Ppmps;

// Domain-internal parameter object. Handlers map PpmpItemRequest → PpmpItemData before calling domain methods.
public sealed record PpmpItemData(
    string GeneralDescription,
    ProjectType ProjectType,
    decimal Quantity,
    string Unit,
    ModeOfProcurement ModeOfProcurement,
    bool PreProcurementConference,
    string ProcurementStart,
    string ProcurementEnd,
    string ExpectedDelivery,
    string SourceOfFunds,
    decimal EstimatedBudget,
    string? SupportingDocuments,
    string? Remarks);

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

    internal static PpmpItem Create(Guid ppmpId, int itemNo, PpmpItemData data) =>
        new()
        {
            Id = Guid.NewGuid(),
            PpmpId = ppmpId,
            ItemNo = itemNo,
            GeneralDescription = data.GeneralDescription,
            ProjectType = data.ProjectType,
            Quantity = data.Quantity,
            Unit = data.Unit,
            ModeOfProcurement = data.ModeOfProcurement,
            PreProcurementConference = data.PreProcurementConference,
            ProcurementStart = data.ProcurementStart,
            ProcurementEnd = data.ProcurementEnd,
            ExpectedDelivery = data.ExpectedDelivery,
            SourceOfFunds = data.SourceOfFunds,
            EstimatedBudget = data.EstimatedBudget,
            SupportingDocuments = data.SupportingDocuments,
            Remarks = data.Remarks
        };

    internal static PpmpItem Clone(Guid ppmpId, int itemNo, PpmpItem source) =>
        new()
        {
            Id = Guid.NewGuid(),
            PpmpId = ppmpId,
            ItemNo = itemNo,
            GeneralDescription = source.GeneralDescription,
            ProjectType = source.ProjectType,
            Quantity = source.Quantity,
            Unit = source.Unit,
            ModeOfProcurement = source.ModeOfProcurement,
            PreProcurementConference = source.PreProcurementConference,
            ProcurementStart = source.ProcurementStart,
            ProcurementEnd = source.ProcurementEnd,
            ExpectedDelivery = source.ExpectedDelivery,
            SourceOfFunds = source.SourceOfFunds,
            EstimatedBudget = source.EstimatedBudget,
            SupportingDocuments = source.SupportingDocuments,
            Remarks = source.Remarks
        };
}

public sealed class Ppmp : AggregateRoot<Guid>, IAuditableEntity, ISoftDeletable
{
    public string PpmpNumber { get; private set; } = default!;
    public int FiscalYear { get; private set; }
    public PpmpPhase Phase { get; private set; }
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
    public Guid? AmendedById { get; private set; }

    public Guid PreparedById { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public Guid? ApprovedById { get; private set; }

    public string? ReturnReason { get; private set; }
    public DateTimeOffset? ReturnedAt { get; private set; }
    public Guid? ReturnedById { get; private set; }

    // Set when this PPMP is consolidated into an APP
    public Guid? AppId { get; private set; }

    public byte[] Version { get; private set; } = [];

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

    private void MarkChanged()
    {
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    public static Ppmp Create(
        string ppmpNumber,
        int fiscalYear,
        PpmpPhase phase,
        string officeCode,
        string endUserUnit,
        Guid preparedById,
        IEnumerable<PpmpItemData> items)
    {
        var ppmp = new Ppmp
        {
            Id = Guid.NewGuid(),
            PpmpNumber = ppmpNumber,
            FiscalYear = fiscalYear,
            Phase = phase,
            OfficeCode = officeCode,
            EndUserUnit = endUserUnit,
            PreparedById = preparedById,
            Status = PpmpStatus.Draft,
            VersionNumber = 1,
            IsCurrentVersion = true,
            VersionChainId = Guid.NewGuid(),
            PreviousVersionId = null,
            CreatedOnUtc = DateTimeOffset.UtcNow,
            Version = NewVersion()
        };

        ppmp.ReplaceItems(items);
        return ppmp;
    }

    public void Update(
        int fiscalYear,
        string officeCode,
        string endUserUnit,
        Guid preparedById,
        IEnumerable<PpmpItemData> items)
    {
        if (Status is not (PpmpStatus.Draft or PpmpStatus.Returned))
            throw new InvalidOperationException("Only Draft or Returned PPMPs can be updated.");

        FiscalYear = fiscalYear;
        OfficeCode = officeCode;
        EndUserUnit = endUserUnit;
        PreparedById = preparedById;

        ReplaceItems(items);
        MarkChanged();
    }

    public void Submit()
    {
        if (Status is not (PpmpStatus.Draft or PpmpStatus.Returned))
            throw new InvalidOperationException("Only Draft or Returned PPMPs can be submitted.");
        if (_items.Count == 0)
            throw new InvalidOperationException("PPMP must have at least one item before submitting.");

        Status = PpmpStatus.Submitted;
        SubmittedAt = DateTimeOffset.UtcNow;
        MarkChanged();
    }

    public void Approve(Guid approvedById)
    {
        if (Status != PpmpStatus.Submitted)
            throw new InvalidOperationException("Only Submitted PPMPs can be approved.");

        Status = PpmpStatus.Approved;
        ApprovedById = approvedById;
        ApprovedAt = DateTimeOffset.UtcNow;
        MarkChanged();
    }

    public void Recall()
    {
        if (Status != PpmpStatus.Submitted)
            throw new InvalidOperationException("Only Submitted PPMPs can be recalled.");

        Status = PpmpStatus.Draft;
        SubmittedAt = null;
        MarkChanged();
    }

    public void Return(string returnReason, Guid returnedById)
    {
        if (Status != PpmpStatus.Submitted)
            throw new InvalidOperationException("Only Submitted PPMPs can be returned for revision.");

        Status = PpmpStatus.Returned;
        ReturnReason = returnReason;
        ReturnedAt = DateTimeOffset.UtcNow;
        ReturnedById = returnedById;
        MarkChanged();
    }

    public void MarkConsolidated(Guid appId)
    {
        if (Status != PpmpStatus.Approved)
            throw new InvalidOperationException("Only Approved PPMPs can be consolidated into an APP.");

        Status = PpmpStatus.Consolidated;
        AppId = appId;
        MarkChanged();
    }

    public void UnmarkConsolidated()
    {
        if (Status != PpmpStatus.Consolidated)
            throw new InvalidOperationException("Only Consolidated PPMPs can be unmarked.");

        Status = PpmpStatus.Approved;
        AppId = null;
        MarkChanged();
    }

    /// <summary>Promotes an Approved Indicative PPMP to a new Final draft. Caller must call Supersede() on this instance.</summary>
    public Ppmp PromoteToFinal(Guid promotedById)
    {
        if (Status is not PpmpStatus.Approved)
            throw new InvalidOperationException("Only Approved PPMPs can be promoted to Final.");
        if (Phase is not PpmpPhase.Indicative)
            throw new InvalidOperationException("Only Indicative PPMPs can be promoted to Final.");

        var finalPpmp = new Ppmp
        {
            Id = Guid.NewGuid(),
            PpmpNumber = PpmpNumber,
            FiscalYear = FiscalYear,
            Phase = PpmpPhase.Final,
            OfficeCode = OfficeCode,
            EndUserUnit = EndUserUnit,
            PreparedById = PreparedById,
            Status = PpmpStatus.Draft,
            VersionNumber = 1,
            IsCurrentVersion = true,
            VersionChainId = Guid.NewGuid(),
            PreviousVersionId = Id,
            AmendedAt = DateTimeOffset.UtcNow,
            AmendedById = promotedById,
            CreatedOnUtc = DateTimeOffset.UtcNow,
            Version = NewVersion()
        };

        var itemNo = 1;
        foreach (var item in _items)
            finalPpmp._items.Add(PpmpItem.Clone(finalPpmp.Id, itemNo++, item));

        return finalPpmp;
    }

    /// <summary>Creates a new Updated version of an Approved/Consolidated Final or Updated PPMP. Caller must call Supersede() on this instance.</summary>
    public Ppmp CreateUpdate(string updateReason, Guid updatedById)
    {
        if (Status is not (PpmpStatus.Approved or PpmpStatus.Consolidated))
            throw new InvalidOperationException("Only Approved or Consolidated PPMPs can have an update created.");
        if (Phase is PpmpPhase.Indicative)
            throw new InvalidOperationException("Indicative PPMPs should be promoted to Final, not updated directly.");

        var update = new Ppmp
        {
            Id = Guid.NewGuid(),
            PpmpNumber = PpmpNumber,
            FiscalYear = FiscalYear,
            Phase = PpmpPhase.Updated,
            OfficeCode = OfficeCode,
            EndUserUnit = EndUserUnit,
            PreparedById = PreparedById,
            Status = PpmpStatus.Draft,
            VersionNumber = VersionNumber + 1,
            IsCurrentVersion = true,
            VersionChainId = VersionChainId,
            PreviousVersionId = Id,
            AmendmentReason = updateReason,
            AmendedAt = DateTimeOffset.UtcNow,
            AmendedById = updatedById,
            CreatedOnUtc = DateTimeOffset.UtcNow,
            Version = NewVersion()
        };

        var itemNo = 1;
        foreach (var item in _items)
            update._items.Add(PpmpItem.Clone(update.Id, itemNo++, item));

        return update;
    }

    public void Supersede()
    {
        if (!IsCurrentVersion)
            throw new InvalidOperationException("Only the current version can be superseded.");
        if (Status == PpmpStatus.Superseded)
            throw new InvalidOperationException("PPMP is already superseded.");

        IsCurrentVersion = false;
        Status = PpmpStatus.Superseded;
        MarkChanged();
    }

    private void ReplaceItems(IEnumerable<PpmpItemData> items)
    {
        _items.Clear();
        var itemNo = 1;
        foreach (var d in items)
            _items.Add(PpmpItem.Create(Id, itemNo++, d));
    }
}
