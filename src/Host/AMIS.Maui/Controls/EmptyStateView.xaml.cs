using System.Windows.Input;

namespace AMIS.Maui.Controls;

/// <summary>
/// Reusable empty/blank-state panel: a Material Symbols glyph, a title, an optional hint line,
/// and an optional action button. Use as a <c>CollectionView.EmptyView</c> or inline in place of
/// the bare-string empty views used elsewhere. Set <see cref="Glyph"/> from
/// <see cref="Helpers.MaterialIcons"/>.
/// </summary>
public partial class EmptyStateView : ContentView
{
    public EmptyStateView()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty GlyphProperty =
        BindableProperty.Create(nameof(Glyph), typeof(string), typeof(EmptyStateView), Helpers.MaterialIcons.Inbox);

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(EmptyStateView));

    public static readonly BindableProperty HintProperty =
        BindableProperty.Create(nameof(Hint), typeof(string), typeof(EmptyStateView));

    public static readonly BindableProperty ActionTextProperty =
        BindableProperty.Create(nameof(ActionText), typeof(string), typeof(EmptyStateView));

    public static readonly BindableProperty ActionCommandProperty =
        BindableProperty.Create(nameof(ActionCommand), typeof(ICommand), typeof(EmptyStateView));

    public string? Glyph
    {
        get => (string?)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Hint
    {
        get => (string?)GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }

    public string? ActionText
    {
        get => (string?)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }
}
