using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views;

public partial class ProductFormPage : ContentPage, IQueryAttributable
{
    // From this width the photo stands beside the fields
    private const double WideWidth = 840;

    private readonly ProductFormViewModel _viewModel;
    private bool? _wide;

    public ProductFormPage(ProductFormViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // On a phone the camera preview makes room while something is typed
        if (DeviceInfo.Current.Idiom == DeviceIdiom.Phone)
        {
            BarcodeField.InputFocusChanged += OnTypingFocusChanged;
            NameField.InputFocusChanged += OnTypingFocusChanged;
        }
    }

    // The Shell hands navigation parameters to the page; the ViewModel (in Core) receives them
    public void ApplyQueryAttributes(IDictionary<string, object> query) => _viewModel.ApplyQueryAttributes(query);

    // Hardware back button: the same command as the on-screen back arrow and Cancel
    protected override bool OnBackButtonPressed()
    {
        _viewModel.CancelCommand.Execute(null);
        return true;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0 || _wide == width >= WideWidth)
        {
            return;
        }

        _wide = width >= WideWidth;
        Columns.ColumnDefinitions = _wide.Value
            ? [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)]
            : [new ColumnDefinition(GridLength.Star)];
        Grid.SetRow(PhotoColumn, _wide.Value ? 0 : 1);
        Grid.SetColumn(PhotoColumn, _wide.Value ? 1 : 0);
    }

    private void OnTypingFocusChanged(object? sender, EventArgs e) =>
        Dispatcher.Dispatch(() => _viewModel.IsTyping = BarcodeField.IsInputFocused || NameField.IsInputFocused);
}
