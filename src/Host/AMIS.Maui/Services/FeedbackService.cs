using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace AMIS.Maui.Services;

/// <summary>
/// Lightweight, non-blocking user feedback: toasts, snackbars and haptics. Replaces blocking
/// <c>DisplayAlert</c> calls for success/info messages (confirmations that need a decision stay as
/// alerts). Every method swallows its own failures — feedback must never crash a flow (haptics in
/// particular throw on Windows and on devices without a vibration motor).
/// </summary>
public interface IFeedbackService
{
    Task ShowToastAsync(string message);
    Task ShowSnackbarAsync(string message, string actionText, Func<Task> action);
    void Success();
    void Click();
    void Warning();
}

public sealed class FeedbackService : IFeedbackService
{
    public async Task ShowToastAsync(string message)
    {
        try
        {
            await Toast.Make(message, ToastDuration.Short).Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Toast failed: {ex}");
        }
    }

    public async Task ShowSnackbarAsync(string message, string actionText, Func<Task> action)
    {
        try
        {
            await Snackbar.Make(
                message,
                action: () => _ = SafeInvoke(action),
                actionButtonText: actionText,
                duration: TimeSpan.FromSeconds(4)).Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Snackbar failed: {ex}");
        }
    }

    public void Success() => TryHaptic(HapticFeedbackType.LongPress);

    public void Click() => TryHaptic(HapticFeedbackType.Click);

    public void Warning() => TryHaptic(HapticFeedbackType.LongPress);

    private static void TryHaptic(HapticFeedbackType type)
    {
        try
        {
            HapticFeedback.Default.Perform(type);
        }
        catch (Exception ex)
        {
            // FeatureNotSupportedException on Windows / no-motor devices, among others.
            System.Diagnostics.Debug.WriteLine($"Haptic failed: {ex}");
        }
    }

    private static async Task SafeInvoke(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Snackbar action failed: {ex}");
        }
    }
}
