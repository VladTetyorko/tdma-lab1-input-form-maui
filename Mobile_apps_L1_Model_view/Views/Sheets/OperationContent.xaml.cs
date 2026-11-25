using Mobile_apps_L1_Model_view.ViewModels.Sheets;

namespace Mobile_apps_L1_Model_view.Views.Sheets;

public partial class OperationContent : ContentView
{
    public OperationContent()
    {
        InitializeComponent();
        RightField.Entry.Focused += (_, _) => Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), () =>
        {
            RightField.Entry.CursorPosition = 0;
            RightField.Entry.SelectionLength = RightField.Entry.Text?.Length ?? 0;
        });
    }

    // Step 2: the field has the cursor with the booked number selected, so typing replaces it
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is OperationSheetViewModel viewModel)
        {
            viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(OperationSheetViewModel.IsCorrecting) && viewModel.IsCorrecting)
                {
                    Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(250), RightField.FocusInput);
                }
            };
        }
    }
}
