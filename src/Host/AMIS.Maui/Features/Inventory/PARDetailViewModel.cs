using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AMIS.Maui.Services;

namespace AMIS.Maui.Features.Inventory;

[QueryProperty(nameof(Id), "Id")]
public sealed partial class PARDetailViewModel(IApiClient apiClient, IFeedbackService feedback) : ObservableObject
{
    [ObservableProperty] public partial string Id { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAccept))]
    public partial PARDetailDto? Detail { get; set; }

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
            Detail = await apiClient.GetPARByIdAsync(guid, ct);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Could not load PAR details. Check your connection.";
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

        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Accept PAR",
            $"Accept accountability for {Detail?.PARNo}? This confirms you have received the listed property.",
            "Accept", "Cancel");
        if (!confirmed) return;

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await apiClient.AcceptAccountabilityAsync(guid, ct);
            feedback.Success();
            await feedback.ShowToastAsync("PAR accepted — now active.");
            Detail = await apiClient.GetPARByIdAsync(guid, ct);
        }
        catch (OperationCanceledException)
        {
            // Superseded or navigated away — nothing to report.
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Could not accept the PAR. Check your connection and try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
