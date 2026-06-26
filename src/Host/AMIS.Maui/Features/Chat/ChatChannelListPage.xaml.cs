namespace AMIS.Maui.Features.Chat;

public partial class ChatChannelListPage : ContentPage
{
    public ChatChannelListPage(ChatChannelListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ChatChannelListViewModel vm)
            vm.LoadCommand.Execute(null);
    }
}
