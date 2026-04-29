using System.Security.Cryptography;
using FSH.Framework.Core.Domain;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using FSH.Modules.ProcurementPlanning.Domain.Ppmps;

namespace FSH.Modules.ProcurementPlanning.Domain.AnnualProcurementPlans;

// AppItem is a point-in-time snapshot of a PpmpItem at consolidation. It is intentionally
// denormalized — if the source PPMP is later amended, AppItem preserves the original values.
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
    public AppPhase Phase { get; private set; }
    public AppStatus Status { get; private set; }

    // Versioning
    public int VersionNumber { get; private set; }
    public bool IsCurrentVersion { get; private set; }
    public Guid VersionChainId { get; private set; }
    public Guid? PreviousVersionId { get; private set; }
    public string? AmendmentReason { get; private set; }
    public DateTimeOffset? AmendedAt { get; private set; }
    public Guid? AmendedById { get; private set; }

    public Guid? ConsolidatedById { get; private set; }
    public DateTimeOffset? ConsolidatedOn { get; private set; }
    public Guid? ApprovedById { get; private set; }
    public DateTimeOffset? ApprovedOn { get; private set; }

    public string? ReturnReason { get; private set; }
    public DateTimeOffset? ReturnedAt { get; private set; }
    public Guid? ReturnedById { get; private set; }

    public byte[] Version { get; private set; } = [];

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

    private static byte[] NewVersion() => RandomNumberGenerator.GetBytes(8);

    private void MarkChanged()
    {
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    public static AnnualProcurementPlan Create(string appNumber, int fiscalYear, AppPhase phase) =>
        new()
        {
            Id = Guid.NewGuid(),
            AppNumber = appNumber,
            FiscalYear = fiscalYear,
            Phase = phase,
            Status = AppStatus.Draft,
            VersionNumber = 1,
            IsCurrentVersion = true,
            VersionChainId = Guid.NewGuid(),
            CreatedOnUtc = DateTimeOffset.UtcNow,
            Version = NewVersion()
        };

    /// <summary>Consolidates approved PPMPs into this APP. Re-consolidating the same PPMPs replaces their items.</summary>
    public void ConsolidatePpmps(IEnumerable<Ppmp> ppmps, Guid consolidatedById)
    {
        if (Status is not (AppStatus.Draft or AppStatus.Returned))
            throw new InvalidOperationException("Only Draft or Returned APPs can have PPMPs consolidated into them.");

        var ppmpList = ppmps.ToList();

        var expectedPpmpPhase = (PpmpPhase)(int)Phase;
        var mismatch = ppmpList.FirstOrDefault(p => p.Phase != expectedPpmpPhase);
        if (mismatch is not null)
            throw new InvalidOperationException(
                $"PPMP {mismatch.Id} has phase '{mismatch.Phase}' but this APP requires phase '{expectedPpmpPhase}'.");

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
        MarkChanged();
    }

    public void Publish()
    {
        if (Status is not (AppStatus.Draft or AppStatus.Returned))
            throw new InvalidOperationException("Only Draft or Returned APPs can be submitted for approval.");
        if (_items.Count == 0)
            throw new InvalidOperationException("APP must have at least one item before publishing.");

        Status = AppStatus.Published;
        MarkChanged();
    }

    public void Approve(Guid approvedById)
    {
        if (Status != AppStatus.Published)
            throw new InvalidOperationException("Only Published APPs can be approved.");

        Status = AppStatus.Approved;
        ApprovedById = approvedById;
        ApprovedOn = DateTimeOffset.UtcNow;
        MarkChanged();
    }

    public void Recall()
    {
        if (Status != AppStatus.Published)
            throw new InvalidOperationException("Only Published APPs can be recalled.");

        Status = AppStatus.Draft;
        ReturnReason = null;
        ReturnedAt = null;
        ReturnedById = null;
        MarkChanged();
    }

    public void Return(string returnReason, Guid returnedById)
    {
        if (Status != AppStatus.Published)
            throw new InvalidOperationException("Only Published APPs can be returned for revision.");

        Status = AppStatus.Returned;
        ReturnReason = returnReason;
        ReturnedAt = DateTimeOffset.UtcNow;
        ReturnedById = returnedById;
        MarkChanged();
    }

    /// <summary>Promotes an Approved Indicative APP to a new empty Final draft. BAC Sec must re-consolidate Final PPMPs into the new APP.</summary>
    public AnnualProcurementPlan PromoteToFinal(Guid promotedById)
    {
        if (Status is not AppStatus.Approved)
            throw new InvalidOperationException("Only Approved APPs can be promoted to Final.");
        if (Phase is not AppPhase.Indicative)
            throw new InvalidOperationException("Only Indicative APPs can be promoted to Final.");

        return new AnnualProcurementPlan
        {
            Id = Guid.NewGuid(),
            AppNumber = AppNumber,
            FiscalYear = FiscalYear,
            Phase = AppPhase.Final,
            Status = AppStatus.Draft,
            VersionNumber = 1,
            IsCurrentVersion = true,
            VersionChainId = Guid.NewGuid(),
            PreviousVersionId = Id,
            AmendedAt = DateTimeOffset.UtcNow,
            AmendedById = promotedById,
            CreatedOnUtc = DateTimeOffset.UtcNow,
            Version = NewVersion()
        };
    }

    /// <summary>Creates a new Updated version of an Approved Final or Updated APP. Caller must call Supersede() on this instance.</summary>
    public AnnualProcurementPlan CreateUpdate(string updateReason, Guid updatedById)
    {
        if (Status is not (AppStatus.Published or AppStatus.Approved))
            throw new InvalidOperationException("Only Published or Approved APPs can have an update created.");
        if (Phase is AppPhase.Indicative)
            throw new InvalidOperationException("Indicative APPs should be promoted to Final, not updated directly.");

        var update = new AnnualProcurementPlan
        {
            Id = Guid.NewGuid(),
            AppNumber = AppNumber,
            FiscalYear = FiscalYear,
            Phase = AppPhase.Updated,
            Status = AppStatus.Draft,
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
        foreach (var src in _items)
            update._items.Add(AppItem.Clone(update.Id, itemNo++, src));

        return update;
    }

    public void Supersede()
    {
        if (!IsCurrentVersion)
            throw new InvalidOperationException("Only the current version can be superseded.");
        if (Status == AppStatus.Superseded)
            throw new InvalidOperationException("APP is already superseded.");

        IsCurrentVersion = false;
        Status = AppStatus.Superseded;
        MarkChanged();
    }
}
