namespace AMIS.Maui.Controls;

/// <summary>
/// Rounded leading tile that shows an asset's photo or a type-tinted placeholder glyph.
/// Extracted from the ICS/PAR detail and checklist item templates, which all embedded the
/// same 52x52 tile verbatim. Bind <see cref="Item"/> to the whole DTO (the image converter
/// reads HasImage + AssetRegistryId off it), plus <see cref="AssetType"/> and
/// <see cref="HasImage"/> for the tint and photo/placeholder toggle.
/// </summary>
public partial class AssetThumbnail : ContentView
{
    public AssetThumbnail()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty ItemProperty =
        BindableProperty.Create(nameof(Item), typeof(object), typeof(AssetThumbnail));

    public static readonly BindableProperty AssetTypeProperty =
        BindableProperty.Create(nameof(AssetType), typeof(string), typeof(AssetThumbnail));

    public static readonly BindableProperty HasImageProperty =
        BindableProperty.Create(nameof(HasImage), typeof(bool), typeof(AssetThumbnail), false);

    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(nameof(Size), typeof(double), typeof(AssetThumbnail), 52d);

    public object? Item
    {
        get => GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    public string? AssetType
    {
        get => (string?)GetValue(AssetTypeProperty);
        set => SetValue(AssetTypeProperty, value);
    }

    public bool HasImage
    {
        get => (bool)GetValue(HasImageProperty);
        set => SetValue(HasImageProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }
}
