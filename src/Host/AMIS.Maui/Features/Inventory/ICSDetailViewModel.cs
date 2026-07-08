using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AMIS.Maui.Services;

namespace AMIS.Maui.Features.Inventory;

[QueryProperty(nameof(Id), "Id")]
public sealed partial class ICSDetailViewModel(IApiClient apiClient, IFeedbackService feedback) : ObservableObject
{
    [ObservableProperty] public partial string Id { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAccept))]
    public partial ICSDetailDto? Detail { get; set; }

    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    // The Accept action is offered only while the document awaits the accountable person's
    // acknowledgement; once Active it's hidden.
    public bool CanAccept => string.Equals(Detail?.Status, "PendingAcceptance", StringComparison.OrdinalIgnoreCase);

    partial void OnIdChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct = default)
    {
        if (!Guid.TryParse(Id, out var guid)) return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            Detail = await apiClient.GetICSByIdAsync(guid, ct);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Could not load ICS details. Check your connection.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AcceptAsync(CancellationToken ct = default)
    {
        if (!Guid.TryParse(Id, out var guid) || !CanAccept) return;

        var confirmed = await Shell.Current.DisplayAlert(
            "Accept ICS",
            $"Accept accountability for {Detail?.ICSNo}? This confirms you have received the listed property.",
            "Accept", "Cancel");
        if (!confirmed) return;

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await apiClient.AcceptAccountabilityAsync(guid, ct);
            feedback.Success();
            await feedback.ShowToastAsync("ICS accepted — now active.");
            Detail = await apiClient.GetICSByIdAsync(guid, ct);
        }
        catch (OperationCanceledException) { }
        catch (HttpRequestException)
        {
            ErrorMessage = "Could not accept the ICS. Check your connection and try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
