using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views.Components;

// Windows: Up, Down and Esc in the search box move through and close the drop-down (a MAUI Entry has no key events).
// Enter is the Entry's ReturnCommand. On other platforms this does nothing: rows are tapped
public sealed class QuickSearchKeysBehavior(Func<QuickSearchViewModel?> viewModel) : Behavior<Entry>
{
#if WINDOWS
    private Microsoft.UI.Xaml.Controls.TextBox? _box;

    protected override void OnAttachedTo(Entry entry)
    {
        base.OnAttachedTo(entry);
        entry.HandlerChanged += OnHandlerChanged;
        Hook(entry);
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        entry.HandlerChanged -= OnHandlerChanged;
        Unhook();
        base.OnDetachingFrom(entry);
    }

    private void OnHandlerChanged(object? sender, EventArgs e) => Hook((Entry)sender!);

    private void Hook(Entry entry)
    {
        Unhook();
        _box = entry.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox;
        if (_box is not null)
        {
            _box.PreviewKeyDown += OnKeyDown;
        }
    }

    private void Unhook()
    {
        if (_box is not null)
        {
            _box.PreviewKeyDown -= OnKeyDown;
            _box = null;
        }
    }

    private void OnKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (viewModel() is not { IsOpen: true } search)
        {
            return;
        }

        System.Windows.Input.ICommand? command = e.Key switch
        {
            Windows.System.VirtualKey.Down => search.MoveNextCommand,
            Windows.System.VirtualKey.Up => search.MovePreviousCommand,
            Windows.System.VirtualKey.Escape => search.CloseCommand,
            _ => null
        };
        if (command is not null)
        {
            command.Execute(null);
            e.Handled = true;
        }
    }
#else
    // Kept so the constructor argument is used on every platform
    protected Func<QuickSearchViewModel?> ViewModel => viewModel;
#endif
}
