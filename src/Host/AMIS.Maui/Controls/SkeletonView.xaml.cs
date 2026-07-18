using Microsoft.Maui.Controls.Shapes;

namespace AMIS.Maui.Controls;

/// <summary>
/// Pulsing placeholder shown during first load in place of a bare spinner. Renders
/// <see cref="RowCount"/> list-style rows (a rounded tile + two bars) and loops a gentle opacity
/// pulse while attached. Opacity-only animation keeps it cheap on low-end Android; the loop is
/// cancelled on unload so it never runs off-screen.
/// </summary>
// CA1001: _cts is owned by the Loaded/Unloaded pair below — OnUnloaded cancels, disposes and nulls it,
// so the token source never outlives the view. MAUI never disposes a ContentView, so implementing
// IDisposable here would add plumbing that nothing calls.
#pragma warning disable CA1001
public partial class SkeletonView : ContentView
#pragma warning restore CA1001
{
    private CancellationTokenSource? _cts;

    public SkeletonView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public static readonly BindableProperty RowCountProperty =
        BindableProperty.Create(nameof(RowCount), typeof(int), typeof(SkeletonView), 5,
            propertyChanged: (b, _, _) => ((SkeletonView)b).BuildRows());

    public int RowCount
    {
        get => (int)GetValue(RowCountProperty);
        set => SetValue(RowCountProperty, value);
    }

    private static Color BarColor =>
        Application.Current?.RequestedTheme == AppTheme.Dark
            ? Color.FromArgb("#404040")   // Gray600
            : Color.FromArgb("#E1E1E1");  // Gray100

    private void BuildRows()
    {
        RowsHost.Children.Clear();
        var bar = BarColor;

        for (var i = 0; i < RowCount; i++)
        {
            var tile = new Border
            {
                WidthRequest = 52,
                HeightRequest = 52,
                Stroke = Colors.Transparent,
                BackgroundColor = bar,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                VerticalOptions = LayoutOptions.Center
            };

            var line1 = new Border
            {
                HeightRequest = 12,
                Stroke = Colors.Transparent,
                BackgroundColor = bar,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                HorizontalOptions = LayoutOptions.Fill
            };

            var line2 = new Border
            {
                HeightRequest = 12,
                WidthRequest = 160,
                Stroke = Colors.Transparent,
                BackgroundColor = bar,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                HorizontalOptions = LayoutOptions.Start
            };

            var lines = new VerticalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
            lines.Children.Add(line1);
            lines.Children.Add(line2);

            var row = new Grid
            {
                ColumnSpacing = 12,
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star)
                }
            };
            row.Add(tile, 0);
            row.Add(lines, 1);

            RowsHost.Children.Add(row);
        }
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (RowsHost.Children.Count == 0)
        {
            BuildRows();
        }

        if (_cts is not null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
        }

        _cts = new CancellationTokenSource();
        await PulseAsync(_cts.Token);
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async Task PulseAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await RowsHost.FadeToAsync(0.45, 900, Easing.SinInOut);
                if (ct.IsCancellationRequested) return;
                await RowsHost.FadeToAsync(1.0, 900, Easing.SinInOut);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the view is unloaded mid-animation.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SkeletonView pulse failed: {ex}");
        }
    }
}
