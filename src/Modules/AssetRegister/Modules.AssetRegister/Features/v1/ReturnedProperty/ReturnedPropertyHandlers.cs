using System.Globalization;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ReturnedProperty;
using AMIS.Modules.AssetRegister.Contracts.v1.SignedDocuments;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.ReturnedProperty;
using AMIS.Modules.AssetRegister.Features.v1.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.ReturnedProperty;

public sealed class CreateReturnedPropertyReceiptCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<CreateReturnedPropertyReceiptCommand, ReturnedPropertyReceiptDto>
{
    public async ValueTask<ReturnedPropertyReceiptDto> Handle(
        CreateReturnedPropertyReceiptCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        // 1. Load accountability with lines.
        var accountability = await db.PropertyAccountabilities
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == cmd.AccountabilityId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Accountability '{cmd.AccountabilityId}' not found.");

        if (accountability.Status != AccountabilityStatus.Active)
            throw new InvalidOperationException(
                $"Accountability '{accountability.DocumentNo}' is {accountability.Status}. Only Active accountabilities can have returns requested.");

        // Validate receipt type matches accountability type.
        var expectedType = cmd.ReceiptType == ReturnedPropertyReceiptType.RRSP
            ? AccountabilityType.SE_ICS
            : AccountabilityType.PPE_PAR;
        if (accountability.AccountabilityType != expectedType)
            throw new InvalidOperationException(
                $"Receipt type {cmd.ReceiptType} requires a {expectedType} accountability, but '{accountability.DocumentNo}' is {accountability.AccountabilityType}.");

        // 2. Resolve selected lines.
        var selectedLineIds = cmd.AccountabilityLineIds.ToHashSet();
        var selectedLines = accountability.Lines
            .Where(l => selectedLineIds.Contains(l.Id))
            .ToList();

        var unknownIds = selectedLineIds.Except(selectedLines.Select(l => l.Id)).ToList();
        if (unknownIds.Count > 0)
            throw new InvalidOperationException(
                $"Unknown accountability line IDs: {string.Join(", ", unknownIds)}.");

        var notActive = selectedLines.Where(l => l.LineStatus != AccountabilityLineStatus.Active).ToList();
        if (notActive.Count > 0)
            throw new InvalidOperationException(
                $"Lines are not in Active status: {string.Join(", ", notActive.Select(l => l.Id))}.");

        // 3. Guard: a line cannot have two outstanding (Pending) return requests at once.
        var alreadyPending = await db.ReturnedPropertyReceipts
            .Where(r => r.Status == ReturnedPropertyReceiptStatus.Pending)
            .SelectMany(r => r.Items)
            .Where(i => selectedLineIds.Contains(i.AccountabilityLineId))
            .Select(i => i.AccountabilityLineId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (alreadyPending.Count > 0)
            throw new InvalidOperationException(
                "One or more selected items already have a pending return request. Resolve it before requesting again.");

        // 4. Load assets for snapshots.
        var assetIds = selectedLines.Select(l => l.AssetRegistryId).ToList();
        var assets = await db.AssetRegistries
            .Where(a => assetIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken).ConfigureAwait(false);

        // 5. Create the pending request — NO asset/accountability mutation yet.
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var returnedBy = EmployeeRef.Create(cmd.ReturnedBy.EmployeeId, cmd.ReturnedBy.PrintedName, cmd.ReturnedBy.Designation);
        var assignedInspector = EmployeeRef.Create(cmd.Inspector.EmployeeId, cmd.Inspector.PrintedName, cmd.Inspector.Designation);

        var receipt = ReturnedPropertyReceipt.Create(
            tenantId, cmd.ReceiptType, cmd.Date,
            cmd.AccountabilityId, accountability.DocumentNo,
            returnedBy, assignedInspector, cmd.Remarks);

        db.ReturnedPropertyReceipts.Add(receipt);

        for (var i = 0; i < selectedLines.Count; i++)
        {
            var line = selectedLines[i];
            if (!assets.TryGetValue(line.AssetRegistryId, out var asset))
                throw new KeyNotFoundException($"Asset '{line.AssetRegistryId}' not found in registry.");

            receipt.AddItem(line.Id, asset.Id, i + 1, asset.Snapshot());
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await db.Entry(receipt).Collection(r => r.Items).LoadAsync(cancellationToken).ConfigureAwait(false);
        return ReturnedPropertyMapper.ToDto(receipt);
    }
}

public sealed class InspectReturnedPropertyReceiptCommandHandler(
    AssetRegisterDbContext db,
    ICurrentUser currentUser,
    IMediator mediator)
    : ICommandHandler<InspectReturnedPropertyReceiptCommand, ReturnedPropertyReceiptDto>
{
    public async ValueTask<ReturnedPropertyReceiptDto> Handle(
        InspectReturnedPropertyReceiptCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var receipt = await db.ReturnedPropertyReceipts
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Return request '{cmd.Id}' not found.");

        // The inspector is the authenticated user — resolved from identity, never trusted from the payload.
        var employee = await CurrentEmployeeResolver.TryResolveAsync(currentUser, mediator, cancellationToken).ConfigureAwait(false)
            ?? throw new ForbiddenException("No employee profile is linked to your account, so you cannot be recorded as the inspector.");

        var inspectedBy = EmployeeRef.Create(
            employee.Id,
            $"{employee.FirstName} {employee.LastName}".Trim(),
            string.IsNullOrWhiteSpace(employee.PositionName) ? null : employee.PositionName);

        var conditionsByItemId = cmd.ItemConditions.ToDictionary(i => i.ItemId, i => i.Condition);

        try
        {
            receipt.Inspect(inspectedBy, conditionsByItemId, cmd.Remarks);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Authenticated, but not the inspector the requester nominated → 403.
            throw new ForbiddenException(ex.Message);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ReturnedPropertyMapper.ToDto(receipt);
    }
}

public sealed class ReassignReturnedPropertyInspectorCommandHandler(
    AssetRegisterDbContext db,
    ICurrentUser currentUser,
    IMediator mediator)
    : ICommandHandler<ReassignReturnedPropertyInspectorCommand, ReturnedPropertyReceiptDto>
{
    public async ValueTask<ReturnedPropertyReceiptDto> Handle(
        ReassignReturnedPropertyInspectorCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var receipt = await db.ReturnedPropertyReceipts
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Return request '{cmd.Id}' not found.");

        // Reassigning the nominated inspector is the requester's own action (custodian override for cleanup),
        // matching the withdraw rule. The endpoint gates on Create; this stops acting on another's request.
        await ReturnedPropertyScope.EnsureCanActAsRequesterAsync(
            currentUser, mediator, receipt.ReturnedBy.EmployeeId, "reassign its inspector", cancellationToken).ConfigureAwait(false);

        var newInspector = EmployeeRef.Create(cmd.Inspector.EmployeeId, cmd.Inspector.PrintedName, cmd.Inspector.Designation);
        receipt.ReassignInspector(newInspector);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ReturnedPropertyMapper.ToDto(receipt);
    }
}

public sealed class AcceptReturnedPropertyReceiptCommandHandler(
    AssetRegisterDbContext db,
    Domain.Services.ICountFreezeGuard freezeGuard,
    Domain.Services.IUnserviceableReportNumberGenerator unserviceableNumbers)
    : ICommandHandler<AcceptReturnedPropertyReceiptCommand, ReturnedPropertyReceiptDto>
{
    public async ValueTask<ReturnedPropertyReceiptDto> Handle(
        AcceptReturnedPropertyReceiptCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var receipt = await db.ReturnedPropertyReceipts
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Return request '{cmd.Id}' not found.");

        if (receipt.Status != ReturnedPropertyReceiptStatus.Inspected)
            throw new InvalidOperationException(
                $"Return request is {receipt.Status}. Only Inspected requests can be accepted.");

        // Load the accountability with lines so we can flip assets back to Available now.
        var accountability = await db.PropertyAccountabilities
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == receipt.AccountabilityId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Accountability '{receipt.AccountabilityId}' not found.");

        // Each item carries the condition the inspector assessed it at; carry that through to the asset and the line.
        var conditionByLine = receipt.Items.ToDictionary(
            i => i.AccountabilityLineId,
            i => i.InspectedCondition ?? AssetCondition.InGoodCondition);
        var lineIds = conditionByLine.Keys.ToHashSet();
        var lines = accountability.Lines.Where(l => lineIds.Contains(l.Id)).ToList();

        var notActive = lines.Where(l => l.LineStatus != AccountabilityLineStatus.Active).ToList();
        if (notActive.Count > 0)
            throw new InvalidOperationException(
                "One or more items are no longer Active on the accountability (they may have been returned through another document). Reject this request and raise a new one.");

        // Load the assets we're about to move.
        var assetIds = lines.Select(l => l.AssetRegistryId).ToList();
        var assets = await db.AssetRegistries
            .Where(a => assetIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken).ConfigureAwait(false);
        await freezeGuard.EnsureMovementAllowedAsync(assets.Values.ToList(), cancellationToken).ConfigureAwait(false);

        var receivedBy = EmployeeRef.Create(cmd.ReceivedBy.EmployeeId, cmd.ReceivedBy.PrintedName, cmd.ReceivedBy.Designation);

        // Queue anything the inspector assessed as Unserviceable into a Draft IIRUSP/IIRUP so the
        // custodian doesn't have to hunt for it afterwards (mirrors the manual candidate picker).
        // Runs before any asset mutation so the number generator's counter allocation is the only
        // thing flushed early. NOTE: assets are NOT flipped to the Unserviceable lifecycle here —
        // that happens only when the custodian submits the report.
        await QueueUnserviceableCandidatesAsync(receipt, lines, assets, conditionByLine, receivedBy, cancellationToken).ConfigureAwait(false);

        // Flip assets back to Available at their inspected condition.
        foreach (var line in lines)
        {
            if (assets.TryGetValue(line.AssetRegistryId, out var asset))
            {
                asset.ReturnToAvailable();
                asset.UpdateCondition(conditionByLine[line.Id]);
            }
        }

        // Close the accountability lines (and the accountability if fully returned), each at its inspected condition.
        accountability.ReturnLines(
            lines.Select(l => (l.Id, conditionByLine[l.Id], (int?)null)),
            receipt.Date);

        // Assign the official receipt number and capture the receiver.
        var receiptNo = await GenerateReceiptNoAsync(receipt.ReceiptType, receipt.Date.Year, cancellationToken).ConfigureAwait(false);
        receipt.Accept(receiptNo, receivedBy);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ReturnedPropertyMapper.ToDto(receipt);
    }

    /// <summary>
    /// Adds every returned item the inspector flagged Unserviceable to a Draft unserviceable property
    /// report (IIRUSP for RRSP/SE, IIRUP for RRP/PPE). Appends to the newest open Draft of that type
    /// — a rolling worklist — or opens a new one seeded from the first candidate asset and the
    /// receiving custodian. Items already present are skipped, so accepting is idempotent against the
    /// draft. Does not submit the report or change asset lifecycle; the custodian reviews and submits.
    /// </summary>
    private async Task QueueUnserviceableCandidatesAsync(
        Domain.ReturnedProperty.ReturnedPropertyReceipt receipt,
        List<Domain.Accountability.PropertyAccountabilityLine> lines,
        Dictionary<Guid, Domain.Assets.AssetRegistry> assets,
        Dictionary<Guid, AssetCondition> conditionByLine,
        EmployeeRef receivedBy,
        CancellationToken cancellationToken)
    {
        var unserviceableLines = lines
            .Where(l => conditionByLine.TryGetValue(l.Id, out var c) && c == AssetCondition.Unserviceable)
            .ToList();
        if (unserviceableLines.Count == 0)
            return;

        var reportType = receipt.ReceiptType == ReturnedPropertyReceiptType.RRSP
            ? UnserviceableReportType.IIRUSP
            : UnserviceableReportType.IIRUP;

        // A disposal report is filed under exactly one fund cluster (the report derives it from its first
        // item and rejects any that disagree). So the rolling worklist is per (reportType, cluster): group
        // the returned candidates by the asset's cluster and find-or-open one draft for each group.
        var linesByCluster = unserviceableLines
            .Where(l => assets.ContainsKey(l.AssetRegistryId))
            .GroupBy(l => assets[l.AssetRegistryId].FundCluster);

        foreach (var group in linesByCluster)
        {
            var fundCluster = group.Key;
            var draft = await db.UnserviceablePropertyReports
                .Include(r => r.Items)
                .Where(r => r.ReportType == reportType
                    && r.Status == UnserviceableReportStatus.Draft
                    && r.FundCluster == fundCluster)
                .OrderByDescending(r => r.CreatedOnUtc)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (draft is null)
            {
                var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
                var reportNo = await unserviceableNumbers.NextAsync(reportType, receipt.Date, cancellationToken).ConfigureAwait(false);
                // The cluster is stamped by the first AddItem below, not passed to CreateDraft.
                draft = Domain.Unserviceable.UnserviceablePropertyReport.CreateDraft(
                    tenantId, reportNo, reportType, station: string.Empty, receipt.Date, receivedBy);
                db.UnserviceablePropertyReports.Add(draft);
            }

            foreach (var line in group)
            {
                var asset = assets[line.AssetRegistryId];
                if (draft.Items.Any(i => i.AssetRegistryId == asset.Id))
                    continue; // already queued (e.g. added manually) — don't duplicate
                draft.AddItem(asset, $"Auto-queued from {receipt.ReceiptType} return of {receipt.AccountabilityDocumentNo}.");
            }
        }
    }

    private async Task<string> GenerateReceiptNoAsync(ReturnedPropertyReceiptType type, int year, CancellationToken cancellationToken)
    {
        var prefix = type == ReturnedPropertyReceiptType.RRSP ? "RRSP" : "RRP";
        var seqPrefix = $"{prefix}-{year.ToString(CultureInfo.InvariantCulture)}-";

        var existing = await db.ReturnedPropertyReceipts
            .Where(r => r.ReceiptNo != null && r.ReceiptNo.StartsWith(seqPrefix))
            .Select(r => r.ReceiptNo!)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var maxSeq = existing
            .Select(n => int.TryParse(n[seqPrefix.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{seqPrefix}{(maxSeq + 1).ToString("D5", CultureInfo.InvariantCulture)}";
    }
}

public sealed class RejectReturnedPropertyReceiptCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<RejectReturnedPropertyReceiptCommand, ReturnedPropertyReceiptDto>
{
    public async ValueTask<ReturnedPropertyReceiptDto> Handle(
        RejectReturnedPropertyReceiptCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var receipt = await db.ReturnedPropertyReceipts
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Return request '{cmd.Id}' not found.");

        receipt.Reject(cmd.Reason);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ReturnedPropertyMapper.ToDto(receipt);
    }
}

public sealed class CancelReturnedPropertyReceiptCommandHandler(
    AssetRegisterDbContext db,
    ICurrentUser currentUser,
    IMediator mediator)
    : ICommandHandler<CancelReturnedPropertyReceiptCommand, ReturnedPropertyReceiptDto>
{
    public async ValueTask<ReturnedPropertyReceiptDto> Handle(
        CancelReturnedPropertyReceiptCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var receipt = await db.ReturnedPropertyReceipts
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Return request '{cmd.Id}' not found.");

        // The endpoint gates on Create; this additionally stops one requester withdrawing another's request
        // (custodian override for cleanup). Actor is resolved from identity, never the payload.
        await ReturnedPropertyScope.EnsureCanActAsRequesterAsync(
            currentUser, mediator, receipt.ReturnedBy.EmployeeId, "withdraw it", cancellationToken).ConfigureAwait(false);

        receipt.Cancel(cmd.Reason);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ReturnedPropertyMapper.ToDto(receipt);
    }
}

public sealed class GetReturnedPropertyReceiptQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetReturnedPropertyReceiptQuery, ReturnedPropertyReceiptDto?>
{
    public async ValueTask<ReturnedPropertyReceiptDto?> Handle(
        GetReturnedPropertyReceiptQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var receipt = await db.ReturnedPropertyReceipts
            .AsNoTracking()
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken).ConfigureAwait(false);
        return receipt is null ? null : ReturnedPropertyMapper.ToDto(receipt);
    }
}

public sealed class SearchReturnedPropertyReceiptsQueryHandler(
    AssetRegisterDbContext db,
    ICurrentUser currentUser,
    IMediator mediator)
    : IQueryHandler<SearchReturnedPropertyReceiptsQuery, PagedResponse<ReturnedPropertyReceiptSummaryDto>>
{
    public async ValueTask<PagedResponse<ReturnedPropertyReceiptSummaryDto>> Handle(
        SearchReturnedPropertyReceiptsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = db.ReturnedPropertyReceipts
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.ToLowerInvariant();
            q = q.Where(r => (r.ReceiptNo != null && r.ReceiptNo.ToLower().Contains(k))
                           || r.AccountabilityDocumentNo.ToLower().Contains(k));
        }

        if (query.ReceiptType.HasValue) q = q.Where(r => r.ReceiptType == query.ReceiptType.Value);
        if (query.Status.HasValue)      q = q.Where(r => r.Status == query.Status.Value);
        // Non-privileged callers are hard-scoped to their own requests server-side, regardless of the
        // requester id they pass; inspectors/custodians may see all (or filter to a chosen requester).
        var requesterFilter = await ReturnedPropertyScope
            .ResolveRequesterFilterAsync(currentUser, mediator, query.ReturnedByEmployeeId, cancellationToken).ConfigureAwait(false);
        if (requesterFilter is { } scopedId)
            q = q.Where(r => r.ReturnedBy.EmployeeId == scopedId);
        if (query.FromDate.HasValue)    q = q.Where(r => r.Date >= query.FromDate.Value);
        if (query.ToDate.HasValue)      q = q.Where(r => r.Date <= query.ToDate.Value);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 15 : query.PageSize;

        var total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var items = await q.OrderByDescending(r => r.Date)
            .ThenByDescending(r => r.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(r => new ReturnedPropertyReceiptSummaryDto(
                r.Id,
                r.ReceiptNo,
                r.ReceiptType,
                r.Status,
                r.Date,
                r.AccountabilityDocumentNo,
                r.Items.Count,
                r.Items.Sum(i => i.Snapshot.UnitCost),
                r.AssignedInspector.EmployeeId,
                r.SignedCopy != null,
                r.ReturnedBy.EmployeeId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new PagedResponse<ReturnedPropertyReceiptSummaryDto>
        {
            Items      = items,
            PageNumber = pageNumber,
            PageSize   = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }
}

public sealed class GetReturnedPropertyStatusCountsQueryHandler(
    AssetRegisterDbContext db,
    ICurrentUser currentUser,
    IMediator mediator)
    : IQueryHandler<GetReturnedPropertyStatusCountsQuery, IReadOnlyList<ReturnedPropertyStatusCountDto>>
{
    public async ValueTask<IReadOnlyList<ReturnedPropertyStatusCountDto>> Handle(
        GetReturnedPropertyStatusCountsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = db.ReturnedPropertyReceipts.AsNoTracking().AsQueryable();

        // Counts must match the list the caller can actually see (same server-side scoping as the search).
        var requesterFilter = await ReturnedPropertyScope
            .ResolveRequesterFilterAsync(currentUser, mediator, query.ReturnedByEmployeeId, cancellationToken).ConfigureAwait(false);
        if (requesterFilter is { } scopedId)
            q = q.Where(r => r.ReturnedBy.EmployeeId == scopedId);
        if (query.FromDate.HasValue) q = q.Where(r => r.Date >= query.FromDate.Value);
        if (query.ToDate.HasValue)   q = q.Where(r => r.Date <= query.ToDate.Value);

        return await q.GroupBy(r => r.Status)
            .Select(g => new ReturnedPropertyStatusCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
