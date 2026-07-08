namespace AMIS.Maui.Controls;

/// <summary>
/// Small tinted pill for status/type labels. Replaces the hand-rolled chip Borders that were
/// duplicated across the ICS/PAR detail, checklist, session-list and channel-list templates.
/// </summary>
public partial class StatusPill : ContentView
{
    public StatusPill()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(StatusPill));

    public static readonly BindableProperty PillBackgroundProperty =
        BindableProperty.Create(nameof(PillBackground), typeof(Color), typeof(StatusPill), Colors.Transparent);

    public static readonly BindableProperty PillTextColorProperty =
        BindableProperty.Create(nameof(PillTextColor), typeof(Color), typeof(StatusPill), Colors.Black);

    public static readonly BindableProperty FontSizeProperty =
        BindableProperty.Create(nameof(FontSize), typeof(double), typeof(StatusPill), 10d);

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Color PillBackground
    {
        get => (Color)GetValue(PillBackgroundProperty);
        set => SetValue(PillBackgroundProperty, value);
    }

    public Color PillTextColor
    {
        get => (Color)GetValue(PillTextColorProperty);
        set => SetValue(PillTextColorProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }
}
