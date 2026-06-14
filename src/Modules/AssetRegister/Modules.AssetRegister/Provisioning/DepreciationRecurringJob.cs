using AMIS.Modules.AssetRegister.Data.Services;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.AssetRegister.Provisioning;

/// <summary>
/// Hangfire recurring job that posts monthly COA straight-line depreciation for all PPE assets
/// across every tenant. Runs monthly; safe to re-run — already-posted months are skipped and any
/// missed months are caught up on the next run.
/// </summary>
public sealed class DepreciationRecurringJob(
    DepreciationPostingService service,
    ILogger<DepreciationRecurringJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var period = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var result = await service.PostAllTenantsThroughAsync(period, cancellationToken).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "DepreciationRecurringJob: posted {Entries} depreciation entries across {Assets} PPE asset(s) for {Period:yyyy-MM}.",
                result.EntriesPosted, result.AssetsProcessed, result.Period);
        }
    }
}
