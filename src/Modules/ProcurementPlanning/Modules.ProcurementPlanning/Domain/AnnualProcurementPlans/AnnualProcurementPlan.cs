using FSH.Framework.Core.Domain;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using FSH.Modules.ProcurementPlanning.Domain.Ppmps;

namespace FSH.Modules.ProcurementPlanning.Domain.AnnualProcurementPlans;

public sealed class AppItem
{
    public Guid Id { get; private set; }
    public Guid AppId { get; private set; }
    public Guid SourcePpmpId { get; private set; }
    public Guid SourcePpmpItemId { get; private set; }
    public string OfficeCode { get; private set; } = default!;
    public string EndUserUnit { get; private set; } = default!;
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
    public string? Remarks { get; private set; }

    private AppItem() { }

    public static AppItem FromPpmpItem(Guid appId, int itemNo, Ppmp ppmp, PpmpItem source) =>
        new()
        {
            Id = Guid.NewGuid(),
            AppId = appId,
            SourcePpmpId = ppmp.Id,
            SourcePpmpItemId = source.Id,
            OfficeCode = ppmp.OfficeCode,
            EndUserUnit = ppmp.EndUserUnit,
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
            Remarks = source.Remarks
        };

    internal static AppItem Clone(Guid newAppId, int itemNo, AppItem source) =>
        new()
        {
            Id = Guid.NewGuid(),
            AppId = newAppId,
            SourcePpmpId = source.SourcePpmpId,
            SourcePpmpItemId = source.SourcePpmpItemId,
            OfficeCode = source.OfficeCode,
            EndUserUnit = source.EndUserUnit,
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
            Remarks = source.Remarks
        };
}

public sealed class AnnualProcurementPlan : AggregateRoot<Guid>, IAuditableEntity
{
    public string AppNumber { get; private set; } = default!;
    public int FiscalYear { get; private set; }
    public AppRevisionType RevisionType { get; private set; }
    public AppStatus Status { get; private set; }

    // Versioning
    public int VersionNumber { get; private set; }
    public bool IsCurrentVersion { get; private set; }
    public Guid VersionChainId { get; private set; }
    public Guid? PreviousVersionId { get; private set; }
    public string? AmendmentReason { get; private set; }
    public DateTimeOffset? AmendedAt { get; private set; }
    public string? AmendedById { get; private set; }

    public string? ConsolidatedById { get; private set; }
    public DateTimeOffset? ConsolidatedOn { get; private set; }
    public string? ApprovedById { get; private set; }
    public DateTimeOffset? ApprovedOn { get; private set; }

    public byte[] Version { get; set; } = [];

    private readonly List<AppItem> _items = [];
    public IReadOnlyList<AppItem> Items => _items.AsReadOnly();

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    private AnnualProcurementPlan() { }

    public static AnnualProcurementPlan Create(string appNumber, int fiscalYear, AppRevisionType revisionType) =>
        new()
        {
            Id = Guid.NewGuid(),
            AppNumber = appNumber,
            FiscalYear = fiscalYear,
            RevisionType = revisionType,
            Status = AppStatus.Draft,
            VersionNumber = 1,
            IsCurrentVersion = true,
            VersionChainId = Guid.NewGuid(),
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

    /// <summary>Consolidates approved PPMPs into this APP. Re-consolidating the same PPMPs replaces their items.</summary>
    public void ConsolidatePpmps(IEnumerable<Ppmp> ppmps, string consolidatedById)
    {
        if (Status != AppStatus.Draft)
            throw new InvalidOperationException("Only Draft APPs can have PPMPs consolidated into them.");

        var ppmpList = ppmps.ToList();
        var ppmpIds = ppmpList.Select(p => p.Id).ToHashSet();

        // Remove items previously sourced from these PPMPs (allow re-consolidation)
        _items.RemoveAll(i => ppmpIds.Contains(i.SourcePpmpId));

        var nextItemNo = _items.Count == 0 ? 1 : _items.Max(i => i.ItemNo) + 1;

        foreach (var ppmp in ppmpList)
        {
            foreach (var item in ppmp.Items)
            {
                _items.Add(AppItem.FromPpmpItem(Id, nextItemNo++, ppmp, item));
            }
        }

        ConsolidatedById = consolidatedById;
        ConsolidatedOn = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Publish()
    {
        if (Status != AppStatus.Draft)
            throw new InvalidOperationException("Only Draft APPs can be published.");
        if (_items.Count == 0)
            throw new InvalidOperationException("APP must have at least one item before publishing.");

        Status = AppStatus.Published;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Approve(string approvedById)
    {
        if (Status != AppStatus.Published)
            throw new InvalidOperationException("Only Published APPs can be approved.");

        Status = AppStatus.Approved;
        ApprovedById = approvedById;
        ApprovedOn = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Creates a new version of this APP (amendment). Caller must call Supersede() on this instance.</summary>
    public AnnualProcurementPlan CreateAmendment(string amendmentReason, AppRevisionType revisionType, string amendedById)
    {
        if (Status is not (AppStatus.Published or AppStatus.Approved))
            throw new InvalidOperationException("Only Published or Approved APPs can be amended.");

        var amendment = new AnnualProcurementPlan
        {
            Id = Guid.NewGuid(),
            AppNumber = AppNumber,
            FiscalYear = FiscalYear,
            RevisionType = revisionType,
            Status = AppStatus.Draft,
            VersionNumber = VersionNumber + 1,
            IsCurrentVersion = true,
            VersionChainId = VersionChainId,
            PreviousVersionId = Id,
            AmendmentReason = amendmentReason,
            AmendedAt = DateTimeOffset.UtcNow,
            AmendedById = amendedById,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

        var itemNo = 1;
        foreach (var src in _items)
        {
            amendment._items.Add(AppItem.Clone(amendment.Id, itemNo++, src));
        }

        return amendment;
    }

    public void Supersede()
    {
        IsCurrentVersion = false;
        Status = AppStatus.Superseded;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
