using System.Windows.Input;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views.Components;

// View side of AppBarViewModel. A page sets Route only when it is a root page (Home, Products, Settings)
public partial class AppBar : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string),
        typeof(AppBar), propertyChanged: (bar, _, value) => ((AppBar)bar).TitleText.Text = (string?)value);

    public static readonly BindableProperty BackCommandProperty =
        BindableProperty.Create(nameof(BackCommand), typeof(ICommand), typeof(AppBar));

    public static readonly BindableProperty ActionCommandProperty = BindableProperty.Create(nameof(ActionCommand),
        typeof(ICommand), typeof(AppBar),
        propertyChanged: (bar, _, value) => ((AppBar)bar).ActionButton.Command = (ICommand?)value);

    public static readonly BindableProperty IsActionShownProperty = BindableProperty.Create(nameof(IsActionShown),
        typeof(bool), typeof(AppBar), true, propertyChanged: (bar, _, _) => ((AppBar)bar).ShowAction());

    public AppBar()
    {
        InitializeComponent();
        Loaded += (_, _) => ShowLeftButton();
    }

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Route { get; set; }

    public ICommand? BackCommand
    {
        get => (ICommand?)GetValue(BackCommandProperty);
        set => SetValue(BackCommandProperty, value);
    }

    // The glyph switches the right button on
    public string? ActionGlyph
    {
        get => ActionButton.Text;
        set
        {
            ActionButton.Text = value;
            ShowAction();
        }
    }

    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    // A hidden action leaves its 48 px empty, so the title stays centred
    public bool IsActionShown
    {
        get => (bool)GetValue(IsActionShownProperty);
        set => SetValue(IsActionShownProperty, value);
    }

    // What a screen reader says for the action
    public string? ActionDescription
    {
        get => SemanticProperties.GetDescription(ActionButton);
        set => SemanticProperties.SetDescription(ActionButton, value);
    }

    // AutomationIds can be set only once
    public string? ActionId
    {
        get => ActionButton.AutomationId;
        set => ActionButton.AutomationId = value;
    }

    private void ShowAction() => ActionButton.IsVisible = !string.IsNullOrEmpty(ActionButton.Text) && IsActionShown;

    private void ShowLeftButton()
    {
        var root = Routes.IsRoot(Route);
        LeftButton.Text = (string)Application.Current!.Resources[root ? "IconNavigation" : "IconArrowLeft"];
        SemanticProperties.SetDescription(LeftButton, root ? "Open menu" : "Back");
        LeftButton.AutomationId ??= root ? "MenuButton" : "BackButton";
    }

    private async void OnLeftClicked(object? sender, EventArgs e)
    {
        if (Handler?.MauiContext?.Services.GetService<AppBarViewModel>() is { } viewModel)
        {
            await viewModel.LeftAsync(Route, BackCommand);
        }
    }
}
