using System.Windows.Input;

namespace Mobile_apps_L1_Model_view.Views.Components;

// One place of the bottom bar: icon over label, both always shown; the open one has a soft pill behind its icon.
// The whole column is one button
public sealed class BottomNavItem : ContentView
{
    private readonly Label _glyph = new() { FontFamily = "FluentIcons", FontSize = 22, HorizontalOptions = LayoutOptions.Center };
    private readonly Label _text = new() { FontSize = 11, HorizontalOptions = LayoutOptions.Center, MaxLines = 1 };
    private readonly Border _marker = new()
    {
        StrokeThickness = 0,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
        WidthRequest = 44,
        HeightRequest = 28,
        HorizontalOptions = LayoutOptions.Center
    };
    private readonly Button _button = new();

    public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(nameof(IsSelected), typeof(bool),
        typeof(BottomNavItem), false, propertyChanged: (item, _, _) => ((BottomNavItem)item).Show());

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(nameof(Command), typeof(ICommand),
        typeof(BottomNavItem), propertyChanged: (item, _, value) => ((BottomNavItem)item)._button.Command = (ICommand?)value);

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(nameof(CommandParameter),
        typeof(object), typeof(BottomNavItem),
        propertyChanged: (item, _, value) => ((BottomNavItem)item)._button.CommandParameter = value);

    public BottomNavItem()
    {
        _marker.Content = _glyph;
        _glyph.VerticalOptions = LayoutOptions.Center;
        _button.SetDynamicResource(StyleProperty, "NavOverlay");
        Content = new Grid
        {
            Children =
            {
                new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center, Children = { _marker, _text } },
                _button
            }
        };
        Show();
    }

    public string? Glyph
    {
        get => _glyph.Text;
        set => _glyph.Text = value;
    }

    public string? Text
    {
        get => _text.Text;
        set
        {
            _text.Text = value;
            SemanticProperties.SetDescription(_button, value);
        }
    }

    // AutomationIds can be set only once
    public string? ItemId
    {
        get => _button.AutomationId;
        set => _button.AutomationId = value;
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    // Colour and weight and the marker, so colour is not the only signal
    private void Show()
    {
        var colour = IsSelected ? "PrimaryText" : "TextSecondary";
        _glyph.SetDynamicResource(Label.TextColorProperty, colour);
        _text.SetDynamicResource(Label.TextColorProperty, colour);
        _text.FontFamily = IsSelected ? "OpenSansSemibold" : "OpenSansRegular";
        if (IsSelected)
        {
            _marker.SetDynamicResource(BackgroundProperty, "PrimarySoftBrush");
        }
        else
        {
            _marker.RemoveDynamicResource(BackgroundProperty);
            _marker.Background = Brush.Transparent;
        }
    }
}
