using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Playground.Maui.Services;

namespace Playground.Maui.Features.Auth;

public sealed partial class LoginViewModel(
    IApiClient apiClient,
    ITokenStorageService tokenStorage,
    AuthStateService authState) : ObservableObject
{
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    [RelayCommand]
    private async Task LoginAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter your email and password.";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var tokenResponse = await apiClient.IssueTokenAsync(Email, Password, ct);
            await tokenStorage.SaveTokensAsync(tokenResponse.Token, tokenResponse.RefreshToken);

            var profile = await apiClient.GetMyProfileAsync(ct);
            authState.SetUserProfile(new UserProfile(profile.Id, profile.Email, profile.FirstName, profile.LastName, profile.ImageUrl));

            var employee = await apiClient.GetMyEmployeeAsync(ct);
            authState.SetEmployee(new EmployeeInfo(employee.EmployeeId, employee.FullName, employee.Department, employee.Position));

            Application.Current!.MainPage = new AppShell();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Login failed. Check your credentials and try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
