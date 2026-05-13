using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace AMIS.Framework.Blazor.UI.Components.Dialogs;

public static class AMISDialogService
{
    public static async Task<bool> ShowConfirmAsync(
        this IDialogService dialogService,
        string title,
        string message,
        string confirmText = "Confirm",
        string cancelText = "Cancel",
        Color confirmColor = Color.Primary,
        string icon = Icons.Material.Outlined.Help,
        Color iconColor = Color.Primary)
    {
        ArgumentNullException.ThrowIfNull(dialogService);

        var parameters = new DialogParameters<AMISConfirmDialog>
        {
            { x => x.Title, title },
            { x => x.Message, message },
            { x => x.ConfirmText, confirmText },
            { x => x.CancelText, cancelText },
            { x => x.ConfirmColor, confirmColor },
            { x => x.Icon, icon },
            { x => x.IconColor, iconColor }
        };

        var options = new DialogOptions
        {
            CloseButton = false,
            MaxWidth = MaxWidth.ExtraSmall,
            FullWidth = true,
            BackdropClick = false,
            CloseOnEscapeKey = true
        };

        var dialog = await dialogService.ShowAsync<AMISConfirmDialog>(title, parameters, options);
        var result = await dialog.Result;

        return result is not null && !result.Canceled;
    }

    public static Task<bool> ShowDeleteConfirmAsync(
        this IDialogService dialogService,
        string itemName = "this item")
    {
        return dialogService.ShowConfirmAsync(
            title: "Delete Confirmation",
            message: $"Are you sure you want to delete {itemName}? This action cannot be undone.",
            confirmText: "Delete",
            cancelText: "Cancel",
            confirmColor: Color.Error,
            icon: Icons.Material.Outlined.DeleteForever,
            iconColor: Color.Error);
    }

    public static Task<bool> ShowSignOutConfirmAsync(this IDialogService dialogService)
    {
        return dialogService.ShowConfirmAsync(
            title: "Sign Out",
            message: "Are you sure you want to sign out of your account?",
            confirmText: "Sign Out",
            cancelText: "Cancel",
            confirmColor: Color.Error,
            icon: Icons.Material.Outlined.Logout,
            iconColor: Color.Warning);
    }
}

