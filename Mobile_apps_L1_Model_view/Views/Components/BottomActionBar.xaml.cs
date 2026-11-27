using System.Windows.Input;

namespace Mobile_apps_L1_Model_view.Views.Components;

public partial class BottomActionBar : ContentView
{
    public static readonly BindableProperty TextProperty =
        Property<string>(nameof(Text), (bar, value) => bar.ActionButton.Text = value);

    public static readonly BindableProperty CommandProperty =
        Property<ICommand>(nameof(Command), (bar, value) => bar.ActionButton.Command = value);

    // Why the button is disabled; empty = nothing to explain
    public static readonly BindableProperty CaptionProperty = Property<string>(nameof(Caption), (bar, value) =>
    {
        bar.CaptionText.Text = value;
        bar.CaptionText.IsVisible = !string.IsNullOrEmpty(value);
    });

    public static readonly BindableProperty SecondaryCommandProperty =
        Property<ICommand>(nameof(SecondaryCommand), (bar, value) => bar.SecondaryButton.Command = value);

    public BottomActionBar()
    {
        InitializeComponent();
    }

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public string? Caption
    {
        get => (string?)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public ICommand? SecondaryCommand
    {
        get => (ICommand?)GetValue(SecondaryCommandProperty);
        set => SetValue(SecondaryCommandProperty, value);
    }

    // A text switches the second, outlined button on (desktop only)
    public string? SecondaryText
    {
        get => SecondaryButton.Text;
        set
        {
            SecondaryButton.Text = value;
            SecondaryButton.IsVisible = !string.IsNullOrEmpty(value) && DeviceInfo.Current.Idiom == DeviceIdiom.Desktop;
        }
    }

    // AutomationIds can be set only once
    public string? ButtonId
    {
        get => ActionButton.AutomationId;
        set => ActionButton.AutomationId = value;
    }

    public string? SecondaryId
    {
        get => SecondaryButton.AutomationId;
        set => SecondaryButton.AutomationId = value;
    }

    private static BindableProperty Property<T>(string name, Action<BottomActionBar, T?> changed) where T : class =>
        BindableProperty.Create(name, typeof(T), typeof(BottomActionBar),
            propertyChanged: (bar, _, value) => changed((BottomActionBar)bar, (T?)value));
}
