using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views.Components;

// View side of QuickSearchViewModel (its BindingContext)
public partial class QuickSearchBox : ContentView
{
    // Baymard: 4 to 8 rows on a phone, up to 10 on a desktop
    private const int PhoneRows = 5;
    private const int DesktopRows = 8;

    public QuickSearchBox()
    {
        InitializeComponent();
        Field.Entry.Behaviors.Add(new QuickSearchKeysBehavior(() => BindingContext as QuickSearchViewModel));
        Field.InputFocusChanged += (_, _) => InputFocusChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? InputFocusChanged;

    public string? Label
    {
        get => Field.Label;
        set => Field.Label = value;
    }

    public string? Placeholder
    {
        get => Field.Placeholder;
        set => Field.Placeholder = value;
    }

    public bool IsInputFocused => Field.IsInputFocused;

    public void FocusInput() => Field.FocusInput();

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is QuickSearchViewModel viewModel)
        {
            viewModel.MaxSuggestions = DeviceInfo.Current.Idiom == DeviceIdiom.Phone ? PhoneRows : DesktopRows;
        }
    }
}
