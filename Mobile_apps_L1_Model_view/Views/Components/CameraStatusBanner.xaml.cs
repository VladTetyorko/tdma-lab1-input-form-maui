using System.Windows.Input;

namespace Mobile_apps_L1_Model_view.Views.Components;

public partial class CameraStatusBanner : ContentView
{
    public static readonly BindableProperty MessageProperty = Property<string>(nameof(Message), (banner, value) =>
    {
        banner.MessageText.Text = value;
        banner.IsVisible = !string.IsNullOrEmpty(value);
    });

    public static readonly BindableProperty ActionTextProperty = Property<string>(nameof(ActionText), (banner, value) =>
    {
        banner.ActionButton.Text = value;
        banner.ActionButton.IsVisible = !string.IsNullOrEmpty(value);
    });

    public static readonly BindableProperty ActionCommandProperty =
        Property<ICommand>(nameof(ActionCommand), (banner, value) => banner.ActionButton.Command = value);

    public CameraStatusBanner()
    {
        InitializeComponent();
    }

    public string? Message
    {
        get => (string?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string? ActionText
    {
        get => (string?)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    private static BindableProperty Property<T>(string name, Action<CameraStatusBanner, T?> changed) where T : class =>
        BindableProperty.Create(name, typeof(T), typeof(CameraStatusBanner),
            propertyChanged: (banner, _, value) => changed((CameraStatusBanner)banner, (T?)value));
}
