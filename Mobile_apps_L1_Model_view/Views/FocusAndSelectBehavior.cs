using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views;

// The quantity of the line just added gets the cursor with its text selected, so typing replaces the number. Phone
// only: on a desktop the search box keeps the focus for a keyboard scanner
public sealed class FocusAndSelectBehavior : Behavior<Entry>
{
    protected override void OnAttachedTo(Entry entry)
    {
        base.OnAttachedTo(entry);
        entry.Loaded += OnLoaded;
        entry.Focused += OnFocused;
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        entry.Loaded -= OnLoaded;
        entry.Focused -= OnFocused;
        base.OnDetachingFrom(entry);
    }

    private static void OnLoaded(object? sender, EventArgs e)
    {
        if (sender is Entry { BindingContext: StockDocumentLineItem { WantsFocus: true } } entry
            && DeviceInfo.Current.Idiom == DeviceIdiom.Phone)
        {
            // After the page has finished laying out and the keyboard of the search box has gone
            entry.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(250), () => entry.Focus());
        }
    }

    // Selected after the platform has placed its cursor, so the next digit replaces the whole number
    private static void OnFocused(object? sender, FocusEventArgs e)
    {
        if (sender is Entry entry)
        {
            entry.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(100), () =>
            {
                entry.CursorPosition = 0;
                entry.SelectionLength = entry.Text?.Length ?? 0;
            });
        }
    }
}
