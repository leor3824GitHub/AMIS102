using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AMIS.Maui.Services;
using System.Collections.ObjectModel;

namespace AMIS.Maui.Features.Inventory;

public sealed partial class InventoryViewModel(
    IApiClient apiClient,
    AuthStateService authState) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<ICSSummaryDto> _icsItems = [];
    [ObservableProperty] private ObservableCollection<PARSummaryDto> _parItems = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (authState.Employee is null) return;

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var employeeId = authState.Employee.EmployeeId;
            var icsList = await apiClient.GetMyICSListAsync(employeeId, ct);
            var parList = await apiClient.GetMyPARListAsync(employeeId, ct);

            IcsItems = new ObservableCollection<ICSSummaryDto>(icsList);
            ParItems = new ObservableCollection<PARSummaryDto>(parList);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Could not load data. Pull down to retry.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private static async Task OpenIcsAsync(ICSSummaryDto? item)
    {
        if (item is null) return;
        await Shell.Current.GoToAsync($"{nameof(ICSDetailPage)}?Id={item.Id}");
    }

    [RelayCommand]
    private static async Task OpenParAsync(PARSummaryDto? item)
    {
        if (item is null) return;
        await Shell.Current.GoToAsync($"{nameof(PARDetailPage)}?Id={item.Id}");
    }
}
