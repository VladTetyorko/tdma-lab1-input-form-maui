namespace Mobile_apps_L1_Model_view.Views;

// Gives a control the focus when its page appears, so the user can type at once (quick scan -> number flow).
// For an Entry the current text is selected, so typing replaces the default value.
// Attach to the page and point Target at the control: <views:FocusOnAppearingBehavior Target="{x:Reference QuantityEntry}" />
public sealed class FocusOnAppearingBehavior : Behavior<Page>
{
    public static readonly BindableProperty TargetProperty = BindableProperty.Create(
        nameof(Target), typeof(VisualElement), typeof(FocusOnAppearingBehavior));

    public VisualElement? Target
    {
        get => (VisualElement?)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    protected override void OnAttachedTo(Page page)
    {
        base.OnAttachedTo(page);
        page.Appearing += OnAppearing;
    }

    protected override void OnDetachingFrom(Page page)
    {
        page.Appearing -= OnAppearing;
        base.OnDetachingFrom(page);
    }

    private void OnAppearing(object? sender, EventArgs e)
    {
        if (Target is not { } target)
        {
            return;
        }

        // The control is laid out after Appearing, so focus on the next UI turn
        target.Dispatcher.Dispatch(() =>
        {
            if (target.Focus() && target is Entry { Text.Length: > 0 } entry)
            {
                entry.CursorPosition = 0;
                entry.SelectionLength = entry.Text.Length;
            }
        });
    }
}
