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
        Field.Completed += (_, _) => InputCompleted?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? InputFocusChanged;

    // Enter was pressed in the box
    public event EventHandler? InputCompleted;

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

    // AutomationId of the text box; "QuickSearchEntry" unless the page names its own (can be set only once)
    public string? EntryId
    {
        get => Field.EntryId;
        set => Field.EntryId = value;
    }

    public bool IsInputFocused => Field.IsInputFocused;

    public void FocusInput() => Field.FocusInput();

    public void UnfocusInput() => Field.Entry.Unfocus();

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is QuickSearchViewModel viewModel)
        {
            // Only now are the page's attributes applied for sure (an AutomationId can be set once)
            Field.EntryId ??= "QuickSearchEntry";
            viewModel.MaxSuggestions = DeviceInfo.Current.Idiom == DeviceIdiom.Phone ? PhoneRows : DesktopRows;
        }
    }
}
