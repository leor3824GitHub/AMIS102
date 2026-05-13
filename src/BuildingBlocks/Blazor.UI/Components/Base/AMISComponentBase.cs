using Microsoft.AspNetCore.Components;

namespace AMIS.Framework.Blazor.UI.Components.Base;

public abstract class AMISComponentBase : ComponentBase
{
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
    [Inject] protected IDialogService DialogService { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
}

