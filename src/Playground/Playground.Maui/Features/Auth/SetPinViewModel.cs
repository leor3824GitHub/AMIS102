using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Playground.Maui.Services;

namespace Playground.Maui.Features.Auth;

public sealed partial class SetPinViewModel(
    ITokenStorageService tokenStorage,
    IPinStorageService pinStorage) : ObservableObject
{
    [ObservableProperty] private string _pin = "";
    [ObservableProperty] private string _confirmPin = "";
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isLoading;

    [RelayCommand]
    private async Task SetPinAsync(CancellationToken ct)
    {
        if (Pin.Length < 4)
        {
            ErrorMessage = "PIN must be at least 4 digits.";
            return;
        }
        if (Pin != ConfirmPin)
        {
            ErrorMessage = "PINs do not match. Please try again.";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var (userId, _) = await tokenStorage.GetLastSessionAsync();
            if (userId is null)
            {
                NavigateToShell();
                return;
            }
            await pinStorage.SavePinAsync(Pin, userId);
            NavigateToShell();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private static void Skip() => NavigateToShell();

    private static void NavigateToShell() =>
        Application.Current!.MainPage = new AppShell();
}
