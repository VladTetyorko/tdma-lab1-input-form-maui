using System.Windows.Input;

namespace Mobile_apps_L1_Model_view.Views;

// Runs the command with the row of the entry when the entry loses the focus, so a typed quantity is not lost
// by tapping elsewhere instead of pressing Enter
public sealed class UnfocusCommandBehavior : Behavior<Entry>
{
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(UnfocusCommandBehavior));

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnAttachedTo(Entry entry)
    {
        base.OnAttachedTo(entry);
        entry.Unfocused += OnUnfocused;
        entry.BindingContextChanged += OnBindingContextChanged;
        BindingContext = entry.BindingContext;
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        entry.Unfocused -= OnUnfocused;
        entry.BindingContextChanged -= OnBindingContextChanged;
        base.OnDetachingFrom(entry);
    }

    private void OnBindingContextChanged(object? sender, EventArgs e) => BindingContext = ((Entry)sender!).BindingContext;

    private void OnUnfocused(object? sender, FocusEventArgs e)
    {
        if (sender is Entry { BindingContext: { } row } && Command?.CanExecute(row) == true)
        {
            Command.Execute(row);
        }
    }
}
