using AMIS.Framework.Shared.Persistence;
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
        CreateReturnedPropertyReceiptCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        // 1. Uniqueness check.
        var exists = await db.ReturnedPropertyReceipts
            .AnyAsync(r => r.ReceiptNo == cmd.ReceiptNo, ct).ConfigureAwait(false);
        if (exists)
            throw new InvalidOperationException($"Receipt number '{cmd.ReceiptNo}' already exists.");

        // 2. Load accountability with lines.
        var accountability = await db.PropertyAccountabilities
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == cmd.AccountabilityId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Accountability '{cmd.AccountabilityId}' not found.");

        if (accountability.Status != Contracts.v1.AccountabilityStatus.Active)
            throw new InvalidOperationException(
                $"Accountability '{accountability.DocumentNo}' is {accountability.Status}. Only Active accountabilities can have returns recorded.");

        // Validate receipt type matches accountability type.
        var expectedType = cmd.ReceiptType == Contracts.v1.ReturnedPropertyReceiptType.RRSP
            ? Contracts.v1.AccountabilityType.SE_ICS
            : Contracts.v1.AccountabilityType.PPE_PAR;
        if (accountability.AccountabilityType != expectedType)
            throw new InvalidOperationException(
                $"Receipt type {cmd.ReceiptType} requires a {expectedType} accountability, but '{accountability.DocumentNo}' is {accountability.AccountabilityType}.");

        // 3. Resolve selected lines.
        var selectedLineIds = cmd.AccountabilityLineIds.ToHashSet();
        var selectedLines = accountability.Lines
            .Where(l => selectedLineIds.Contains(l.Id))
            .ToList();

        var unknownIds = selectedLineIds.Except(selectedLines.Select(l => l.Id)).ToList();
        if (unknownIds.Count > 0)
            throw new InvalidOperationException(
                $"Unknown accountability line IDs: {string.Join(", ", unknownIds)}.");

        var notActive = selectedLines.Where(l => l.LineStatus != Contracts.v1.AccountabilityLineStatus.Active).ToList();
        if (notActive.Count > 0)
            throw new InvalidOperationException(
                $"Lines are not in Active status: {string.Join(", ", notActive.Select(l => l.Id))}.");

        // 4. Load assets.
        var assetIds = selectedLines.Select(l => l.AssetRegistryId).ToList();
        var assets = await db.AssetRegistries
            .Where(a => assetIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, ct).ConfigureAwait(false);

        // 5. Build employee refs.
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var returnedBy = EmployeeRef.Create(cmd.ReturnedBy.EmployeeId, cmd.ReturnedBy.PrintedName, cmd.ReturnedBy.Designation);
        EmployeeRef? receivedBy = cmd.ReceivedBy is null ? null
            : EmployeeRef.Create(cmd.ReceivedBy.EmployeeId, cmd.ReceivedBy.PrintedName, cmd.ReceivedBy.Designation);

        // 6. Create receipt.
        var receipt = ReturnedPropertyReceipt.Create(
            tenantId, cmd.ReceiptNo, cmd.ReceiptType, cmd.Date,
            cmd.AccountabilityId, accountability.DocumentNo,
            returnedBy, receivedBy, cmd.Remarks);

        db.ReturnedPropertyReceipts.Add(receipt);

        // 7. Process each line: add item, mark asset returned.
        for (var i = 0; i < selectedLines.Count; i++)
        {
            var line = selectedLines[i];
            if (!assets.TryGetValue(line.AssetRegistryId, out var asset))
                throw new KeyNotFoundException($"Asset '{line.AssetRegistryId}' not found in registry.");

            receipt.AddItem(line.Id, asset.Id, i + 1, asset.Snapshot());
            asset.ReturnToAvailable();
        }

        // 8. Mark lines returned on the accountability and check for full return.
        accountability.ReturnLines(
            selectedLines.Select(l => (l.Id, (int?)null)),
            cmd.Date,
            Contracts.v1.AssetCondition.InGoodCondition);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Reload items for mapping.
        await db.Entry(receipt).Collection(r => r.Items).LoadAsync(ct).ConfigureAwait(false);
        return ReturnedPropertyMapper.ToDto(receipt);
    }
}

public sealed class GetReturnedPropertyReceiptQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetReturnedPropertyReceiptQuery, ReturnedPropertyReceiptDto?>
{
    public async ValueTask<ReturnedPropertyReceiptDto?> Handle(
        GetReturnedPropertyReceiptQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var receipt = await db.ReturnedPropertyReceipts
            .AsNoTracking()
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == query.Id, ct).ConfigureAwait(false);
        return receipt is null ? null : ReturnedPropertyMapper.ToDto(receipt);
    }
}

public sealed class SearchReturnedPropertyReceiptsQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<SearchReturnedPropertyReceiptsQuery, PagedResponse<ReturnedPropertyReceiptSummaryDto>>
{
    public async ValueTask<PagedResponse<ReturnedPropertyReceiptSummaryDto>> Handle(
        SearchReturnedPropertyReceiptsQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = db.ReturnedPropertyReceipts
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.ToLowerInvariant();
            q = q.Where(r => r.ReceiptNo.ToLower().Contains(k)
                           || r.AccountabilityDocumentNo.ToLower().Contains(k));
        }

        if (query.ReceiptType.HasValue) q = q.Where(r => r.ReceiptType == query.ReceiptType.Value);
        if (query.FromDate.HasValue)    q = q.Where(r => r.Date >= query.FromDate.Value);
        if (query.ToDate.HasValue)      q = q.Where(r => r.Date <= query.ToDate.Value);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 15 : query.PageSize;

        var total = await q.LongCountAsync(ct).ConfigureAwait(false);
        var items = await q.OrderByDescending(r => r.Date)
            .ThenByDescending(r => r.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(r => new ReturnedPropertyReceiptSummaryDto(
                r.Id,
                r.ReceiptNo,
                r.ReceiptType,
                r.Date,
                r.AccountabilityDocumentNo,
                r.Items.Count,
                r.Items.Sum(i => i.Snapshot.UnitCost)))
            .ToListAsync(ct).ConfigureAwait(false);

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
