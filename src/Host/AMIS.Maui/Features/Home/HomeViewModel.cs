using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AMIS.Maui.Features.Inventory;
using AMIS.Maui.Features.PhysicalCount;
using AMIS.Maui.Features.Profile;
using AMIS.Maui.Features.Scan;
using AMIS.Maui.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace AMIS.Maui.Features.Home;

// Unified card model for the "Recent" zone — maps either an ICS or a PAR onto one row shape.
public sealed record RecentItem(
    Guid Id,
    string Number,
    string Kind,        // "ICS" | "PAR"
    string Subtitle,    // ICS status, or PAR type
    int ItemCount,
    string Route,       // detail page route
    DateTime SortDate);

public sealed partial class HomeViewModel(
    IApiClient apiClient,
    AuthStateService authState,
    IPhysicalCountSyncService syncService) : ObservableObject
{
    [ObservableProperty] private string _greeting = "Welcome";
    [ObservableProperty] private string _firstName = "";
    [ObservableProperty] private string? _department;
    [ObservableProperty] private string _initials = "?";

    [ObservableProperty] private int _icsCount;
    [ObservableProperty] private int _parCount;
    [ObservableProperty] private int _icsItemCount;
    [ObservableProperty] private int _parItemCount;

    [ObservableProperty] private int _activeSessionCount;
    [ObservableProperty] private bool _hasActiveSession;
    [ObservableProperty] private PhysicalCountSessionSummaryDto? _activeSession;
    [ObservableProperty] private string _activeSessionNo = "";
    [ObservableProperty] private string _activeSessionProgressLabel = "";
    [ObservableProperty] private int _pendingSyncCount;
    [ObservableProperty] private bool _hasPendingSync;

    [ObservableProperty] private ObservableCollection<RecentItem> _recentItems = [];
    [ObservableProperty] private bool _hasRecent;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    partial void OnActiveSessionCountChanged(int value) => HasActiveSession = value > 0;
    partial void OnPendingSyncCountChanged(int value) => HasPendingSync = value > 0;
    partial void OnRecentItemsChanged(ObservableCollection<RecentItem> value) => HasRecent = value.Count > 0;

    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct = default)
    {
        ApplyIdentity();

        if (authState.Employee is null) return;

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var employeeId = authState.Employee.EmployeeId;
            var icsList = await apiClient.GetMyICSListAsync(employeeId, ct);
            var parList = await apiClient.GetMyPARListAsync(employeeId, ct);
            var sessions = await apiClient.GetPhysicalCountSessionsAsync(ct);

            IcsCount = icsList.Count;
            ParCount = parList.Count;
            IcsItemCount = icsList.Sum(i => i.ItemCount);
            ParItemCount = parList.Sum(p => p.ItemCount);

            ApplyActiveSession(sessions);
            ApplyRecent(icsList, parList);

            PendingSyncCount = await syncService.GetPendingCountAsync();
            if (PendingSyncCount > 0 && Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                await syncService.FlushPendingAsync(ct);
                PendingSyncCount = await syncService.GetPendingCountAsync();
            }
        }
        catch (HttpRequestException) when (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            ErrorMessage = "You're offline. Showing what we have.";
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Could not load your dashboard. Pull down to retry.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyIdentity()
    {
        Greeting = DateTime.Now.Hour switch
        {
            < 12 => "Good morning,",
            < 18 => "Good afternoon,",
            _ => "Good evening,"
        };

        var fullName = authState.Employee?.FullName ?? "";
        FirstName = string.IsNullOrWhiteSpace(fullName)
            ? "there"
            : fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        Department = authState.Employee?.Department;
        Initials = ComputeInitials(fullName);
    }

    private void ApplyActiveSession(List<PhysicalCountSessionSummaryDto> sessions)
    {
        // An Ongoing session is one you can still record into — that's what "resume" means.
        var active = sessions
            .Where(s => s.IsOngoing)
            .OrderByDescending(s => s.CountDate)
            .ToList();

        ActiveSessionCount = active.Count;
        ActiveSession = active.FirstOrDefault();

        if (ActiveSession is { } s)
        {
            ActiveSessionNo = s.SessionNo;
            ActiveSessionProgressLabel = s.TotalEntries == 1
                ? "1 entry recorded"
                : $"{s.TotalEntries} entries recorded";
        }
    }

    private void ApplyRecent(List<ICSSummaryDto> icsList, List<PARSummaryDto> parList)
    {
        var recent = icsList
            .Select(i => new RecentItem(
                i.Id, i.ICSNo, "ICS", i.Status, i.ItemCount,
                nameof(ICSDetailPage), ParseDate(i.Date)))
            .Concat(parList.Select(p => new RecentItem(
                p.Id, p.PARNo, "PAR", p.PARType, p.ItemCount,
                nameof(PARDetailPage), ParseDate(p.Date))))
            .OrderByDescending(r => r.SortDate)
            .Take(3);

        RecentItems = new ObservableCollection<RecentItem>(recent);
    }

    [RelayCommand]
    private static Task GoToScanAsync() => Shell.Current.GoToAsync($"//{nameof(ScanPage)}");

    [RelayCommand]
    private static Task GoToCountAsync() => Shell.Current.GoToAsync($"//{nameof(PhysicalCountSessionListPage)}");

    [RelayCommand]
    private static Task GoToInventoryAsync() => Shell.Current.GoToAsync($"//{nameof(InventoryPage)}");

    [RelayCommand]
    private static Task GoToProfileAsync() => Shell.Current.GoToAsync($"//{nameof(ProfilePage)}");

    [RelayCommand]
    private async Task ResumeSessionAsync()
    {
        if (ActiveSession is not { } s) return;

        // Switch to the Count tab first (absolute route resets to the session-list root), then push
        // Scan onto that tab's stack. A single relative route would push the whole count flow onto the
        // Home stack, leaving the tab bar highlighting Home while the user is deep in Scan/Checklist.
        await Shell.Current.GoToAsync($"//{nameof(PhysicalCountSessionListPage)}");
        await Shell.Current.GoToAsync($"{nameof(PhysicalCountScanPage)}?SessionId={s.Id}");
    }

    [RelayCommand]
    private async Task OpenRecentAsync(RecentItem? item)
    {
        if (item is null) return;
        await Shell.Current.GoToAsync($"{item.Route}?Id={item.Id}");
    }

    private static DateTime ParseDate(string value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? dt
            : DateTime.MinValue;

    private static string ComputeInitials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "?";
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => char.ToUpperInvariant(parts[0][0]).ToString(),
            _ => $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}"
        };
    }
}
