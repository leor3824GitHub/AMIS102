using FSH.Framework.Core.Domain;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using FSH.Modules.ProcurementPlanning.Domain.Ppmps;

namespace FSH.Modules.ProcurementPlanning.Domain.AnnualProcurementPlans;

public sealed class AnnualProcurementPlan : AggregateRoot<Guid>, IAuditableEntity, ISoftDeletable
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

    private readonly List<AppSourcePpmp> _sourcePpmps = [];
    public IReadOnlyList<AppSourcePpmp> SourcePpmps => _sourcePpmps.AsReadOnly();

    private readonly List<AppLineItem> _lineItems = [];
    public IReadOnlyList<AppLineItem> LineItems => _lineItems.AsReadOnly();

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    private AnnualProcurementPlan() { }

    private void Touch() => LastModifiedOnUtc = DateTimeOffset.UtcNow;

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
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Validates that this APP can accept the given PPMPs for consolidation.
    /// Throws <see cref="InvalidOperationException"/> if the status or phase constraints are violated.
    /// Does NOT mutate the entity — use this before the persistence-level consolidate operations.
    /// </summary>
    public void ValidateForConsolidation(IEnumerable<Ppmp> ppmps)
    {
        if (Status is not (AppStatus.Draft or AppStatus.Returned))
            throw new InvalidOperationException("Only Draft or Returned APPs can have PPMPs consolidated into them.");

        var mismatch = ppmps.FirstOrDefault(p => !IsPpmpPhaseAllowedForConsolidation(p.Phase));
        if (mismatch is not null)
            throw new InvalidOperationException(
                $"PPMP {mismatch.Id} has phase '{mismatch.Phase}' which is not allowed for APP phase '{Phase}'.");
    }

    /// <summary>Consolidates approved PPMPs into this APP. Re-consolidating the same PPMPs replaces their items.</summary>
    public void ConsolidatePpmps(IEnumerable<Ppmp> ppmps, Guid consolidatedById)
    {
        if (Status is not (AppStatus.Draft or AppStatus.Returned))
            throw new InvalidOperationException("Only Draft or Returned APPs can have PPMPs consolidated into them.");

        var ppmpList = ppmps.ToList();

        var mismatch = ppmpList.FirstOrDefault(p => !IsPpmpPhaseAllowedForConsolidation(p.Phase));
        if (mismatch is not null)
            throw new InvalidOperationException(
                $"PPMP {mismatch.Id} has phase '{mismatch.Phase}' which is not allowed for APP phase '{Phase}'.");

        if (ppmpList.Any(x => x.FiscalYear != FiscalYear))
            throw new InvalidOperationException("All selected PPMPs must belong to the same fiscal year as the APP.");

        var ppmpIds = ppmpList.Select(p => p.Id).ToHashSet();

        _sourcePpmps.RemoveAll(i => ppmpIds.Contains(i.PpmpId));
        _lineItems.RemoveAll(i => ppmpIds.Contains(i.SourcePpmpId));

        var consolidatedOn = DateTimeOffset.UtcNow;
        var nextItemNo = _lineItems.Count == 0 ? 1 : _lineItems.Max(i => i.ItemNo) + 1;

        foreach (var ppmp in ppmpList)
        {
            _sourcePpmps.Add(AppSourcePpmp.FromPpmp(Id, ppmp, consolidatedById, consolidatedOn));

            foreach (var item in ppmp.Items)
            {
                _lineItems.Add(AppLineItem.FromPpmpItem(Id, nextItemNo++, ppmp, item, consolidatedOn));
            }
        }

        ConsolidatedById = consolidatedById;
        ConsolidatedOn = consolidatedOn;
        Touch();
    }

    public void Publish()
    {
        if (Status is not (AppStatus.Draft or AppStatus.Returned))
            throw new InvalidOperationException("Only Draft or Returned APPs can be submitted for approval.");
        if (_lineItems.Count == 0)
            throw new InvalidOperationException("APP must have at least one item before publishing.");

        Status = AppStatus.Published;
        Touch();
    }

    public void Approve(Guid approvedById)
    {
        if (Status != AppStatus.Published)
            throw new InvalidOperationException("Only Published APPs can be approved.");

        Status = AppStatus.Approved;
        ApprovedById = approvedById;
        ApprovedOn = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Recall()
    {
        if (Status != AppStatus.Published)
            throw new InvalidOperationException("Only Published APPs can be recalled.");

        Status = AppStatus.Draft;
        ReturnReason = null;
        ReturnedAt = null;
        ReturnedById = null;
        Touch();
    }

    public void Return(string returnReason, Guid returnedById)
    {
        if (Status != AppStatus.Published)
            throw new InvalidOperationException("Only Published APPs can be returned for revision.");

        Status = AppStatus.Returned;
        ReturnReason = returnReason;
        ReturnedAt = DateTimeOffset.UtcNow;
        ReturnedById = returnedById;
        Touch();
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
            VersionChainId = VersionChainId,
            PreviousVersionId = Id,
            AmendedAt = DateTimeOffset.UtcNow,
            AmendedById = promotedById,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
        // No items — BAC Sec consolidates Final PPMPs fresh into this draft.
    }

    /// <summary>Creates a new Updated version of an Approved Final or Updated APP. Caller must call Supersede() on this instance.</summary>
    public AnnualProcurementPlan CreateUpdate(string updateReason, Guid updatedById)
    {
        if (Status is not AppStatus.Approved)
            throw new InvalidOperationException("Only Approved APPs can have an update created.");
        if (Phase is AppPhase.Indicative)
            throw new InvalidOperationException("Indicative APPs should be promoted to Final, not updated directly.");

        var updatedVersionNumber = Phase == AppPhase.Final
            ? 1
            : VersionNumber + 1;

        var update = new AnnualProcurementPlan
        {
            Id = Guid.NewGuid(),
            AppNumber = AppNumber,
            FiscalYear = FiscalYear,
            Phase = AppPhase.Updated,
            Status = AppStatus.Draft,
            VersionNumber = updatedVersionNumber,
            IsCurrentVersion = true,
            VersionChainId = VersionChainId,
            PreviousVersionId = Id,
            AmendmentReason = updateReason,
            AmendedAt = DateTimeOffset.UtcNow,
            AmendedById = updatedById,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

        foreach (var source in _sourcePpmps)
        {
            update._sourcePpmps.Add(AppSourcePpmp.Clone(update.Id, source));
        }

        var lineNo = 1;
        foreach (var src in _lineItems.OrderBy(x => x.ItemNo))
        {
            update._lineItems.Add(AppLineItem.Clone(update.Id, lineNo++, src));
        }

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
        Touch();
    }

    private bool IsPpmpPhaseAllowedForConsolidation(PpmpPhase ppmpPhase) => Phase switch
    {
        AppPhase.Indicative => ppmpPhase == PpmpPhase.Indicative,
        AppPhase.Final => ppmpPhase == PpmpPhase.Final,
        AppPhase.Updated => ppmpPhase is PpmpPhase.Final or PpmpPhase.Updated,
        _ => false
    };
}
