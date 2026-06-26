namespace AMIS.Maui.Features.Chat;

public partial class ChatConversationPage : ContentPage
{
    private readonly ChatConversationViewModel _vm;

    public ChatConversationPage(ChatConversationViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
        _vm.ScrollToBottomRequested += OnScrollToBottomRequested;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Teardown();
    }

    // KeepItemsInView never auto-scrolls to the newest row, so pin it explicitly. Deferred to the next
    // dispatcher loop so the CollectionView has realized the just-bound items before we scroll.
    private void OnScrollToBottomRequested() => Dispatcher.Dispatch(() =>
    {
        if (_vm.Messages.Count == 0) return;
        MessagesView.ScrollTo(_vm.Messages[^1], position: ScrollToPosition.End, animate: false);
    });
}
