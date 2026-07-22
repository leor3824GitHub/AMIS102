namespace AMIS.Maui.Features.Chat;

public partial class ChatChannelListPage : ContentPage
{
    private readonly ChatChannelListViewModel _vm;

    public ChatChannelListPage(ChatChannelListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Attach();
        _vm.LoadCommand.Execute(null);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Detach();
    }
}
