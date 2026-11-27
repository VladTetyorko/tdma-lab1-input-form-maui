using System.Windows.Input;

namespace Mobile_apps_L1_Model_view.Views.Components;

public partial class OutlinedField : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string),
        typeof(OutlinedField), string.Empty, BindingMode.TwoWay,
        propertyChanged: (field, _, value) => ((OutlinedField)field).ShowText((string?)value));

    public static readonly BindableProperty LabelProperty = Property<string>(nameof(Label), (field, value) =>
    {
        field.CaptionText.Text = value;
        field.CaptionText.IsVisible = !string.IsNullOrEmpty(value);
        SemanticProperties.SetDescription(field.Input, value);
    });

    public static readonly BindableProperty PlaceholderProperty =
        Property<string>(nameof(Placeholder), (field, value) => field.Input.Placeholder = value);

    public static readonly BindableProperty ErrorTextProperty = Property<string>(nameof(ErrorText), (field, value) =>
    {
        field.ErrorLabelView.Text = value;
        field.ErrorLabelView.IsVisible = !string.IsNullOrEmpty(value);
        field.ShowState();
    });

    public static readonly BindableProperty ReturnCommandProperty =
        Property<ICommand>(nameof(ReturnCommand), (field, value) => field.Input.ReturnCommand = value);

    public OutlinedField()
    {
        InitializeComponent();
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsEnabled))
            {
                ShowState();
            }
        };
    }

    // Raised when Enter was pressed in the box
    public event EventHandler? Completed;

    // Raised when the box got or lost the keyboard focus
    public event EventHandler? InputFocusChanged;

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? Label
    {
        get => (string?)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Placeholder
    {
        get => (string?)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public string? ErrorText
    {
        get => (string?)GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }

    public ICommand? ReturnCommand
    {
        get => (ICommand?)GetValue(ReturnCommandProperty);
        set => SetValue(ReturnCommandProperty, value);
    }

    public Keyboard Keyboard
    {
        get => Input.Keyboard;
        set => Input.Keyboard = value;
    }

    public int MaxLength
    {
        get => Input.MaxLength;
        set => Input.MaxLength = value;
    }

    // Hidden characters, with a button that shows them. Such a field has no clear button
    public bool IsPassword
    {
        get => RevealButton.IsVisible;
        set
        {
            Input.IsPassword = value;
            RevealButton.IsVisible = value;
            ShowClear = !value;
        }
    }

    // A narrow box (a quantity) has no room for the clear button
    public bool ShowClear { get; set; } = true;

    // A glyph in front of the text (search)
    public string? Icon
    {
        get => LeadingIcon.Text;
        set
        {
            LeadingIcon.Text = value;
            LeadingIcon.IsVisible = !string.IsNullOrEmpty(value);
        }
    }

    // AutomationIds can be set only once
    public string? EntryId
    {
        get => Input.AutomationId;
        set => Input.AutomationId = value;
    }

    public string? ErrorId
    {
        get => ErrorLabelView.AutomationId;
        set => ErrorLabelView.AutomationId = value;
    }

    public string? ClearId
    {
        get => ClearButton.AutomationId;
        set => ClearButton.AutomationId = value;
    }

    // The native entry, for key handling
    public Entry Entry => Input;

    public bool IsInputFocused => Input.IsFocused;

    public void FocusInput() => Input.Focus();

    private static BindableProperty Property<T>(string name, Action<OutlinedField, T?> changed) where T : class =>
        BindableProperty.Create(name, typeof(T), typeof(OutlinedField),
            propertyChanged: (field, _, value) => changed((OutlinedField)field, (T?)value));

    private void ShowText(string? text)
    {
        if (Input.Text != text)
        {
            Input.Text = text;
        }

        ShowState();
    }

    private void OnInputTextChanged(object? sender, TextChangedEventArgs e)
    {
        Text = e.NewTextValue;
        ShowState();
    }

    private void OnInputFocusChanged(object? sender, FocusEventArgs e)
    {
        ShowState();
        InputFocusChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnInputCompleted(object? sender, EventArgs e) => Completed?.Invoke(this, EventArgs.Empty);

    private void OnRevealClicked(object? sender, EventArgs e)
    {
        Input.IsPassword = !Input.IsPassword;
        RevealButton.Text = (string)Application.Current!.Resources[Input.IsPassword ? "IconEye" : "IconEyeOff"];
        SemanticProperties.SetDescription(RevealButton, Input.IsPassword ? "Show password" : "Hide password");
    }

    private void OnClearClicked(object? sender, EventArgs e)
    {
        Text = string.Empty;
        FocusInput();
    }

    // Disabled wins over error, error over focus
    private void ShowState()
    {
        var error = !string.IsNullOrEmpty(ErrorText);
        var (brush, thickness) =
            !IsEnabled ? ("BorderBrush", 1)
            : error ? ("DangerBrush", Input.IsFocused ? 2 : 1)
            : Input.IsFocused ? ("FocusRingBrush", 2)
            : ("BorderStrongBrush", 1);
        Outline.SetDynamicResource(Border.StrokeProperty, brush);
        Outline.StrokeThickness = thickness;
        Outline.SetDynamicResource(BackgroundProperty, IsEnabled ? "BackgroundBrush" : "SurfaceBrush");
        ClearButton.IsVisible = ShowClear && IsEnabled && !string.IsNullOrEmpty(Input.Text);
    }
}
