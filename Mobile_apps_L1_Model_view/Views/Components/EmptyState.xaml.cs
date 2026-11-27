using System.Windows.Input;

namespace Mobile_apps_L1_Model_view.Views.Components;

public partial class EmptyState : ContentView
{
    public static readonly BindableProperty TitleProperty =
        Property<string>(nameof(Title), (state, value) => state.TitleLabel.Text = value);

    public static readonly BindableProperty MessageProperty = Property<string>(nameof(Message), (state, value) =>
    {
        state.MessageText.Text = value;
        state.MessageText.IsVisible = !string.IsNullOrEmpty(value);
    });

    public static readonly BindableProperty ActionTextProperty = Property<string>(nameof(ActionText), (state, value) =>
    {
        state.ActionButton.Text = value;
        state.ActionButton.IsVisible = !string.IsNullOrEmpty(value);
    });

    public static readonly BindableProperty ActionCommandProperty =
        Property<ICommand>(nameof(ActionCommand), (state, value) => state.ActionButton.Command = value);

    public EmptyState()
    {
        InitializeComponent();
    }

    public string? Glyph
    {
        get => GlyphLabel.Text;
        set => GlyphLabel.Text = value;
    }

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
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

    private static BindableProperty Property<T>(string name, Action<EmptyState, T?> changed) where T : class =>
        BindableProperty.Create(name, typeof(T), typeof(EmptyState),
            propertyChanged: (state, _, value) => changed((EmptyState)state, (T?)value));
}
