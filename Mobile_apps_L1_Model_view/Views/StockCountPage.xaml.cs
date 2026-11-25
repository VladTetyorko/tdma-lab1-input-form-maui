using Mobile_apps_L1_Model_view.ViewModels;
using Mobile_apps_L1_Model_view.Views.Components;

namespace Mobile_apps_L1_Model_view.Views;

// The code here is about focus only
public partial class StockCountPage : ContentPage
{
    private readonly Dictionary<CountRow, BorderlessEntry> _entries = [];

    public StockCountPage(StockCountViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        if (DeviceInfo.Current.Idiom == DeviceIdiom.Phone)
        {
            // Typing in the search box takes the camera's room; typing a count does not (the next scan needs it)
            Search.InputFocusChanged += (_, _) => Dispatcher.Dispatch(() => viewModel.IsTyping = Search.IsInputFocused);
        }

        viewModel.FocusRequested += (_, row) => Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), () => Focus(row));
    }

    private void OnCountLoaded(object? sender, EventArgs e)
    {
        if (sender is BorderlessEntry { BindingContext: CountRow row } entry)
        {
            _entries[row] = entry;
        }
    }

    private async void Focus(CountRow row)
    {
        if (!_entries.TryGetValue(row, out var entry))
        {
            return;
        }

        await Scroller.ScrollToAsync(entry, ScrollToPosition.Center, animated: false);
        entry.Focus();
        entry.CursorPosition = 0;
        entry.SelectionLength = entry.Text?.Length ?? 0;
    }
}
