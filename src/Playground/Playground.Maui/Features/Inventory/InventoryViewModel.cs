using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Playground.Maui.Data.Models;
using Playground.Maui.Services;
using System.Collections.ObjectModel;

namespace Playground.Maui.Features.Inventory;

public sealed partial class InventoryViewModel(
    IApiClient apiClient,
    AuthStateService authState,
    ICacheService cacheService) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<ICSSummaryDto> _icsItems = [];
    [ObservableProperty] private ObservableCollection<PARSummaryDto> _parItems = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _cacheNotice;

    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (authState.Employee is null) return;

        var employeeId = authState.Employee.EmployeeId;

        // Show cached data immediately
        var cachedICS = await cacheService.GetCachedICSAsync(employeeId);
        var cachedPAR = await cacheService.GetCachedPARAsync(employeeId);
        if (cachedICS.Count > 0 || cachedPAR.Count > 0)
        {
            IcsItems = new ObservableCollection<ICSSummaryDto>(cachedICS.Select(MapICS));
            ParItems = new ObservableCollection<PARSummaryDto>(cachedPAR.Select(MapPAR));
            var lastUpdated = cachedICS.Select(x => x.CachedAt).Concat(cachedPAR.Select(x => x.CachedAt))
                .OrderByDescending(x => x).FirstOrDefault();
            if (lastUpdated != default)
                CacheNotice = $"Last updated {(DateTimeOffset.UtcNow - lastUpdated).TotalMinutes:F0} min ago";
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            if (cachedICS.Count == 0 && cachedPAR.Count == 0)
                CacheNotice = "No internet connection and no cached data available.";
            else
                CacheNotice = $"Offline — {CacheNotice}";
            return;
        }

        IsLoading = true;
        try
        {
            var icsList = await apiClient.GetMyICSListAsync(employeeId, ct);
            var parList = await apiClient.GetMyPARListAsync(employeeId, ct);

            IcsItems = new ObservableCollection<ICSSummaryDto>(icsList);
            ParItems = new ObservableCollection<PARSummaryDto>(parList);
            CacheNotice = null;

            await cacheService.UpsertICSAsync(icsList.Select(x => new CachedICS
            {
                Id = x.Id.ToString(),
                ICSNo = x.ICSNo,
                Date = x.Date,
                Status = x.Status,
                ExpiresOn = x.ExpiresOn,
                ItemCount = x.ItemCount,
                EmployeeId = employeeId.ToString(),
                CachedAt = DateTimeOffset.UtcNow,
            }));
            await cacheService.UpsertPARAsync(parList.Select(x => new CachedPAR
            {
                Id = x.Id.ToString(),
                PARNo = x.PARNo,
                Date = x.Date,
                PARType = x.PARType,
                ItemCount = x.ItemCount,
                EmployeeId = employeeId.ToString(),
                CachedAt = DateTimeOffset.UtcNow,
            }));
        }
        catch (HttpRequestException)
        {
            if (cachedICS.Count == 0 && cachedPAR.Count == 0)
                CacheNotice = "Could not load data. Pull down to retry.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static ICSSummaryDto MapICS(CachedICS c) =>
        new(Guid.Parse(c.Id), c.ICSNo, c.Date, c.Status, c.ExpiresOn, c.ItemCount);

    private static PARSummaryDto MapPAR(CachedPAR c) =>
        new(Guid.Parse(c.Id), c.PARNo, c.Date, c.PARType, c.ItemCount);
}
