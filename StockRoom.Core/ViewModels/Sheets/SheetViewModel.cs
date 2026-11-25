using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Mobile_apps_L1_Model_view.ViewModels.Sheets;

// What every sheet has: a question as its title, one main action named after its result, a quiet way back, and one
// answer. Back, a tap outside and a swipe down mean "no", except while the main action is running
public abstract partial class SheetViewModel : ObservableObject
{
    private bool _closed;

    public event EventHandler? Closed;

    // The main action was carried out
    public bool Accepted { get; private set; }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    // "Step 2 of 2"; empty without steps
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStep))]
    public partial string StepText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPrimary))]
    public partial string PrimaryText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSecondary))]
    public partial string SecondaryText { get; set; } = "Cancel";

    // Why the main action is not available, or what went wrong
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasHint))]
    public partial string Hint { get; set; } = string.Empty;

    // The action throws something away: its button is red
    [ObservableProperty]
    public partial bool IsDestructive { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrimaryCommand))]
    [NotifyCanExecuteChangedFor(nameof(SecondaryCommand))]
    public partial bool IsBusy { get; set; }

    public bool HasStep => StepText.Length > 0;

    public bool HasPrimary => PrimaryText.Length > 0;

    public bool HasSecondary => SecondaryText.Length > 0;

    public bool HasHint => Hint.Length > 0;

    protected virtual bool CanAccept => true;

    // Back, scrim, swipe down
    public void Dismiss()
    {
        if (!IsBusy)
        {
            Close(false);
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunPrimary))]
    private async Task PrimaryAsync()
    {
        IsBusy = true;
        bool done;
        try
        {
            done = await OnPrimaryAsync();
        }
        finally
        {
            IsBusy = false;
        }

        if (done)
        {
            Close(true);
        }
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private void Secondary() => OnSecondary();

    // False keeps the sheet open (a next step, or an error)
    protected virtual Task<bool> OnPrimaryAsync() => Task.FromResult(true);

    protected virtual void OnSecondary() => Dismiss();

    protected void Close(bool accepted)
    {
        if (_closed)
        {
            return;
        }

        _closed = true;
        Accepted = accepted;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    // The condition of the main action changed
    protected void RefreshPrimary() => PrimaryCommand.NotifyCanExecuteChanged();

    private bool CanRunPrimary() => !IsBusy && CanAccept;

    private bool IsIdle() => !IsBusy;
}
