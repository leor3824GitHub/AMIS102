using System.Globalization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ReturnedProperty;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.ReturnedProperty;
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

        var receipt = ReturnedPropertyReceipt.Create(
            tenantId, cmd.ReceiptType, cmd.Date,
            cmd.AccountabilityId, accountability.DocumentNo,
            returnedBy, cmd.Remarks);

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

public sealed class AcceptReturnedPropertyReceiptCommandHandler(
    AssetRegisterDbContext db,
    Domain.Services.ICountFreezeGuard freezeGuard)
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

        if (receipt.Status != ReturnedPropertyReceiptStatus.Pending)
            throw new InvalidOperationException(
                $"Return request is {receipt.Status}. Only Pending requests can be accepted.");

        // Load the accountability with lines so we can flip assets back to Available now.
        var accountability = await db.PropertyAccountabilities
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == receipt.AccountabilityId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Accountability '{receipt.AccountabilityId}' not found.");

        var lineIds = receipt.Items.Select(i => i.AccountabilityLineId).ToHashSet();
        var lines = accountability.Lines.Where(l => lineIds.Contains(l.Id)).ToList();

        var notActive = lines.Where(l => l.LineStatus != AccountabilityLineStatus.Active).ToList();
        if (notActive.Count > 0)
            throw new InvalidOperationException(
                "One or more items are no longer Active on the accountability (they may have been returned through another document). Reject this request and raise a new one.");

        // Flip assets back to Available.
        var assetIds = lines.Select(l => l.AssetRegistryId).ToList();
        var assets = await db.AssetRegistries
            .Where(a => assetIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken).ConfigureAwait(false);
        await freezeGuard.EnsureMovementAllowedAsync(assets.Values.ToList(), cancellationToken).ConfigureAwait(false);
        foreach (var line in lines)
        {
            if (assets.TryGetValue(line.AssetRegistryId, out var asset))
                asset.ReturnToAvailable();
        }

        // Close the accountability lines (and the accountability if fully returned).
        accountability.ReturnLines(
            lines.Select(l => (l.Id, (int?)null)),
            receipt.Date,
            AssetCondition.InGoodCondition);

        // Assign the official receipt number and capture the receiver.
        var receiptNo = await GenerateReceiptNoAsync(receipt.ReceiptType, receipt.Date.Year, cancellationToken).ConfigureAwait(false);
        var receivedBy = EmployeeRef.Create(cmd.ReceivedBy.EmployeeId, cmd.ReceivedBy.PrintedName, cmd.ReceivedBy.Designation);
        receipt.Accept(receiptNo, receivedBy);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ReturnedPropertyMapper.ToDto(receipt);
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

public sealed class CancelReturnedPropertyReceiptCommandHandler(AssetRegisterDbContext db)
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

public sealed class SearchReturnedPropertyReceiptsQueryHandler(AssetRegisterDbContext db)
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
        if (query.ReturnedByEmployeeId.HasValue && query.ReturnedByEmployeeId.Value != Guid.Empty)
            q = q.Where(r => r.ReturnedBy.EmployeeId == query.ReturnedByEmployeeId.Value);
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
                r.Items.Sum(i => i.Snapshot.UnitCost)))
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

public sealed class GetReturnedPropertyStatusCountsQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetReturnedPropertyStatusCountsQuery, IReadOnlyList<ReturnedPropertyStatusCountDto>>
{
    public async ValueTask<IReadOnlyList<ReturnedPropertyStatusCountDto>> Handle(
        GetReturnedPropertyStatusCountsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = db.ReturnedPropertyReceipts.AsNoTracking().AsQueryable();

        if (query.ReturnedByEmployeeId.HasValue && query.ReturnedByEmployeeId.Value != Guid.Empty)
            q = q.Where(r => r.ReturnedBy.EmployeeId == query.ReturnedByEmployeeId.Value);
        if (query.FromDate.HasValue) q = q.Where(r => r.Date >= query.FromDate.Value);
        if (query.ToDate.HasValue)   q = q.Where(r => r.Date <= query.ToDate.Value);

        return await q.GroupBy(r => r.Status)
            .Select(g => new ReturnedPropertyStatusCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
