using System.Globalization;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Domain.Services;

namespace AMIS.Modules.AssetRegister.Data.Services;

internal sealed class AccountabilityNumberGenerator(AssetRegisterDbContext db, CounterAllocator allocator) : IAccountabilityNumberGenerator
{
    public async Task<string> NextIcsAsync(AssetCategory category, DateOnly issueDate, CancellationToken ct)
    {
        var prefix = category == AssetCategory.LowValuedSemi ? "SPLV" : "SPHV";
        return await NextAsync(prefix, issueDate, ct).ConfigureAwait(false);
    }

    public Task<string> NextParAsync(DateOnly issueDate, CancellationToken ct) => NextAsync("PAR", issueDate, ct);

    private async Task<string> NextAsync(string prefix, DateOnly date, CancellationToken ct)
    {
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var serial = await allocator.NextSerialAsync(tenantId, date.Year, date.Month, prefix, ct).ConfigureAwait(false);
        return $"{prefix}-{date.Year:D4}-{date.Month:D2}-{serial.ToString("D4", CultureInfo.InvariantCulture)}";
    }
}

internal sealed class InventoryTransferNumberGenerator(AssetRegisterDbContext db, CounterAllocator allocator) : IInventoryTransferNumberGenerator
{
    public async Task<string> NextAsync(DateOnly date, CancellationToken ct)
    {
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var serial = await allocator.NextSerialAsync(tenantId, date.Year, date.Month, "ITR", ct).ConfigureAwait(false);
        return $"ITR-{date.Year:D4}-{date.Month:D2}-{serial.ToString("D4", CultureInfo.InvariantCulture)}";
    }
}

internal sealed class IncidentNumberGenerator(AssetRegisterDbContext db, CounterAllocator allocator) : IIncidentNumberGenerator
{
    public async Task<string> NextAsync(DateOnly incidentDate, CancellationToken ct)
    {
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var serial = await allocator.NextSerialAsync(tenantId, incidentDate.Year, incidentDate.Month, "RLSDDSP", ct).ConfigureAwait(false);
        return $"RLSDDSP-{incidentDate.Year:D4}-{incidentDate.Month:D2}-{serial.ToString("D4", CultureInfo.InvariantCulture)}";
    }
}

internal sealed class IssuanceReportNumberGenerator(AssetRegisterDbContext db, CounterAllocator allocator) : IIssuanceReportNumberGenerator
{
    public async Task<string> NextAsync(IssuanceReportType type, DateOnly periodStart, CancellationToken ct)
    {
        var prefix = type == IssuanceReportType.SMIR ? "RSPI" : "PPEIR";
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var serial = await allocator.NextSerialAsync(tenantId, periodStart.Year, periodStart.Month, prefix, ct).ConfigureAwait(false);
        return $"{prefix}-{periodStart.Year:D4}-{periodStart.Month:D2}-{serial.ToString("D4", CultureInfo.InvariantCulture)}";
    }
}

internal sealed class ReceivingReportNumberGenerator(AssetRegisterDbContext db, CounterAllocator allocator) : IReceivingReportNumberGenerator
{
    public async Task<string> NextAsync(ReceivingDocumentKind kind, DateOnly date, CancellationToken ct)
    {
        var prefix = kind == ReceivingDocumentKind.PPERR ? "PPERR" : "SMRR";
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var serial = await allocator.NextSerialAsync(tenantId, date.Year, date.Month, prefix, ct).ConfigureAwait(false);
        return $"{prefix}-{date.Year:D4}-{date.Month:D2}-{serial.ToString("D4", CultureInfo.InvariantCulture)}";
    }
}

internal sealed class UnserviceableReportNumberGenerator(AssetRegisterDbContext db, CounterAllocator allocator) : IUnserviceableReportNumberGenerator
{
    public async Task<string> NextAsync(UnserviceableReportType type, DateOnly asAt, CancellationToken ct)
    {
        var prefix = type == UnserviceableReportType.IIRUSP ? "IIRUSP" : "IIRUP";
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var serial = await allocator.NextSerialAsync(tenantId, asAt.Year, asAt.Month, prefix, ct).ConfigureAwait(false);
        return $"{prefix}-{asAt.Year:D4}-{asAt.Month:D2}-{serial.ToString("D4", CultureInfo.InvariantCulture)}";
    }
}

/// <summary>
/// Default placeholder CRC calculator: returns acquisition cost. A real implementation
/// would apply COA 2022-004 §4.19 indices; lifted to a dedicated service later.
/// </summary>
internal sealed class CurrentReplacementCostCalculator(AssetRegisterDbContext db) : ICurrentReplacementCostCalculator
{
    public async Task<decimal> ComputeAsync(Guid assetRegistryId, DateOnly asOf, CancellationToken ct)
    {
        _ = asOf;
        var asset = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                db.AssetRegistries, a => a.Id == assetRegistryId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"AssetRegistry '{assetRegistryId}' not found.");
        return asset.UnitCost;
    }
}

