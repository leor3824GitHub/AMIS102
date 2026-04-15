using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.AssetManagement.Provisioning;

/// <summary>
/// Hangfire recurring job that marks Active ICS records as Expired when their
/// <see cref="InventoryCustodianSlip.ExpiresOn"/> date has passed.
///
/// Runs daily. Safe to re-run: already-expired records are skipped.
///
/// COA Circular 2022-004 Section 4.11:
///   - High-valued ICS (SPHV): should be renewed before expiry; if not renewed, this job marks them Expired.
///   - Low-valued ICS (SPLV): expires automatically; property units remain Issued until an RLSDDSP or RRSP is filed.
/// </summary>
public sealed class ICSExpiryJob(AssetManagementDbContext dbContext, ILogger<ICSExpiryJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var expiredIcs = await dbContext.InventoryCustodianSlips
            .Where(x => x.Status == ICSStatus.Active
                     && x.ExpiresOn.HasValue
                     && x.ExpiresOn.Value < today)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (expiredIcs.Count == 0)
        {
            return;
        }

        foreach (var ics in expiredIcs)
        {
            ics.MarkExpired();
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "ICSExpiryJob: marked {Count} ICS record(s) as Expired (run date: {Today})",
                expiredIcs.Count, today);
        }
    }
}
