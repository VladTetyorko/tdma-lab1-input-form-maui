using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views;

// The code here is about focus only
public partial class SearchPage : ContentPage
{
    private readonly SearchViewModel _viewModel;

    public SearchPage(SearchViewModel viewModel, BottomNavViewModel bar)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        if (DeviceInfo.Current.Idiom == DeviceIdiom.Phone)
        {
            // The camera makes room for the results as soon as something is typed, not before
            viewModel.QuickSearch.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(QuickSearchViewModel.Query))
                {
                    _viewModel.IsTyping = viewModel.QuickSearch.Query.Length > 0;
                }
            };
        }

        // "Do we have X?": opened from the bar, the field has the cursor (J4). Coming back from a product page it does
        // not, so the keyboard does not cover the bar again. The page is created by the first such opening
        var focus = true;
        bar.Opening += (_, route) => focus |= route == Routes.Search;
        Appearing += (_, _) =>
        {
            if (focus)
            {
                focus = false;
                Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(300), Field.FocusInput);
            }
        };

        // A tap on the tab that is already open: an empty field with the keyboard
        bar.Reselected += (_, route) =>
        {
            if (route == Routes.Search)
            {
                _viewModel.Reset();
                Field.FocusInput();
            }
        };
    }
}
