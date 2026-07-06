using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AMIS.Maui.Services;
using System.Collections.ObjectModel;

namespace AMIS.Maui.Features.Inventory;

public sealed partial class InventoryViewModel(
    IApiClient apiClient,
    AuthStateService authState) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<InventoryGroup> _groups = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    // Header hero — custodian identity + accountability totals shown on the gradient panel.
    [ObservableProperty] private string _employeeName = "";
    [ObservableProperty] private string? _employeeDetail;
    [ObservableProperty] private int _icsCount;
    [ObservableProperty] private int _parCount;

    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (authState.Employee is null) return;

        EmployeeName = authState.Employee.FullName;
        EmployeeDetail = string.Join("  ·  ",
            new[] { authState.Employee.Position, authState.Employee.Department }
                .Where(s => !string.IsNullOrWhiteSpace(s)))
            is { Length: > 0 } detail ? detail : null;

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var employeeId = authState.Employee.EmployeeId;
            var icsList = await apiClient.GetMyICSListAsync(employeeId, ct);
            var parList = await apiClient.GetMyPARListAsync(employeeId, ct);

            IcsCount = icsList.Count;
            ParCount = parList.Count;

            Groups =
            [
                new InventoryGroup("INVENTORY CUSTODIAN SLIPS", icsList),
                new InventoryGroup("PROPERTY ACKNOWLEDGEMENT RECEIPTS", parList),
            ];
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
