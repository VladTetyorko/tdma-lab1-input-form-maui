using System.Windows.Input;

namespace Mobile_apps_L1_Model_view.Views.Components;

public partial class ActionTile : ContentView
{
    public static readonly BindableProperty TitleProperty = Property<string>(nameof(Title),
        (tile, value) =>
        {
            tile.TitleLabel.Text = value;
            tile.Describe();
        });

    public static readonly BindableProperty CaptionProperty = Property<string>(nameof(Caption),
        (tile, value) =>
        {
            tile.CaptionText.Text = value;
            tile.Describe();
        });

    public static readonly BindableProperty GlyphProperty =
        Property<string>(nameof(Glyph), (tile, value) => tile.GlyphLabel.Text = value);

    public static readonly BindableProperty CommandProperty =
        Property<ICommand>(nameof(Command), (tile, value) => tile.TileButton.Command = value);

    public ActionTile()
    {
        InitializeComponent();
    }

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Caption
    {
        get => (string?)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public string? Glyph
    {
        get => (string?)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    // The AutomationId of the tappable control (can be set only once, as MAUI requires)
    public string? ButtonId
    {
        get => TileButton.AutomationId;
        set => TileButton.AutomationId = value;
    }

    private void Describe() => SemanticProperties.SetDescription(TileButton, $"{Title}. {Caption}");

    private static BindableProperty Property<T>(string name, Action<ActionTile, T?> changed) where T : class =>
        BindableProperty.Create(name, typeof(T), typeof(ActionTile),
            propertyChanged: (tile, _, value) => changed((ActionTile)tile, (T?)value));
}
