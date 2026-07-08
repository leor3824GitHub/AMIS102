namespace AMIS.Maui.Behaviors;

/// <summary>
/// Fades and slides its target in on load — a subtle entrance for hero panels and first content
/// groups. Transform/opacity only (cheap on low-end Android). Set <see cref="Delay"/> to stagger a
/// hero ahead of the content below it. Failures are swallowed so a dropped animation never crashes
/// a page (an unhandled exception here would surface on the UI thread).
/// </summary>
public sealed class EntranceAnimationBehavior : Behavior<VisualElement>
{
    public static readonly BindableProperty DelayProperty =
        BindableProperty.Create(nameof(Delay), typeof(int), typeof(EntranceAnimationBehavior), 0);

    public static readonly BindableProperty OffsetProperty =
        BindableProperty.Create(nameof(Offset), typeof(double), typeof(EntranceAnimationBehavior), 12d);

    public int Delay
    {
        get => (int)GetValue(DelayProperty);
        set => SetValue(DelayProperty, value);
    }

    public double Offset
    {
        get => (double)GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    protected override void OnAttachedTo(VisualElement bindable)
    {
        base.OnAttachedTo(bindable);
        bindable.Loaded += OnLoaded;
    }

    protected override void OnDetachingFrom(VisualElement bindable)
    {
        base.OnDetachingFrom(bindable);
        bindable.Loaded -= OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (sender is not VisualElement view) return;

        try
        {
            view.Opacity = 0;
            view.TranslationY = Offset;

            if (Delay > 0)
                await Task.Delay(Delay);

            await Task.WhenAll(
                view.FadeToAsync(1, 260, Easing.CubicOut),
                view.TranslateToAsync(0, 0, 260, Easing.CubicOut));
        }
        catch (Exception ex)
        {
            // Never let a cosmetic animation take down the page. Restore a sane visible state.
            view.Opacity = 1;
            view.TranslationY = 0;
            System.Diagnostics.Debug.WriteLine($"Entrance animation failed: {ex}");
        }
    }
}
