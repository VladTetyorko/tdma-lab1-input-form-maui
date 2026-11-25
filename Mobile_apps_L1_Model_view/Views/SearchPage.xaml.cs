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
            // Dispatched, so the clear button does not count as "stopped typing"
            Field.InputFocusChanged += (_, _) => Dispatcher.Dispatch(() => _viewModel.IsTyping = Field.IsInputFocused);
        }

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
