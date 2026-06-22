using System.Globalization;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Domain.Services;
using Microsoft.EntityFrameworkCore;

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
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;

        // For PPEIR: use the active pre-printed form series if one exists.
        if (type == IssuanceReportType.PPEIR)
        {
            var activeSeries = await db.PPEIRFormSeries
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.IsActive, ct)
                .ConfigureAwait(false);

            if (activeSeries is not null)
                return activeSeries.AllocateNext();
        }

        // Fall back to date-based auto-generation.
        var prefix = type == IssuanceReportType.SMIR ? "RSPI" : "PPEIR";
        var serial = await allocator.NextSerialAsync(tenantId, periodStart.Year, periodStart.Month, prefix, ct).ConfigureAwait(false);
        return $"{prefix}-{periodStart.Year:D4}-{periodStart.Month:D2}-{serial.ToString("D4", CultureInfo.InvariantCulture)}";
    }
}

internal sealed class ReceivingReportNumberGenerator(AssetRegisterDbContext db, CounterAllocator allocator) : IReceivingReportNumberGenerator
{
    public async Task<string> NextAsync(ReceivingDocumentKind kind, DateOnly date, CancellationToken ct)
    {
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;

        // For PPERR: use the active pre-printed form series if one exists.
        if (kind == ReceivingDocumentKind.PPERR)
        {
            var activeSeries = await db.PPERRFormSeries
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.IsActive, ct)
                .ConfigureAwait(false);

            if (activeSeries is not null)
                return activeSeries.AllocateNext();
        }

        // Fall back to date-based auto-generation.
        var prefix = kind == ReceivingDocumentKind.PPERR ? "PPERR" : "SMRR";
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

