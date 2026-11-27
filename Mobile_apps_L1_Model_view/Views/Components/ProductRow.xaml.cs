using Mobile_apps_L1_Model_view.Services.Search;

namespace Mobile_apps_L1_Model_view.Views.Components;

public partial class ProductRow : ContentView
{
    public static readonly BindableProperty ProductNameProperty = Property(nameof(ProductName), typeof(string),
        (row, value) => row.NameLabel.Value = (string?)value);

    public static readonly BindableProperty CodeProperty = Property(nameof(Code), typeof(string),
        (row, value) => row.CodeLabel.Value = (string?)value);

    public static readonly BindableProperty NameRangesProperty = Property(nameof(NameRanges), typeof(IReadOnlyList<TextRange>),
        (row, value) => row.NameLabel.Ranges = (IReadOnlyList<TextRange>?)value);

    public static readonly BindableProperty CodeRangesProperty = Property(nameof(CodeRanges), typeof(IReadOnlyList<TextRange>),
        (row, value) => row.CodeLabel.Ranges = (IReadOnlyList<TextRange>?)value);

    public static readonly BindableProperty IsOutOfStockProperty = Property(nameof(IsOutOfStock), typeof(bool),
        (row, value) => row.OutChip.IsVisible = value is true);

    // A third caption line: "Similar name", "on hand 24 → 36", why a line is wrong
    public static readonly BindableProperty NoteProperty = Property(nameof(Note), typeof(string), (row, value) =>
    {
        row.NoteLabel.Text = (string?)value;
        row.NoteLabel.IsVisible = !string.IsNullOrEmpty((string?)value);
    });

    // Why the line cannot go as it is, in red with a warning sign
    public static readonly BindableProperty ProblemProperty = Property(nameof(Problem), typeof(string), (row, value) =>
    {
        row.ProblemLabel.Text = (string?)value;
        row.ProblemRow.IsVisible = !string.IsNullOrEmpty((string?)value);
    });

    public string? Problem
    {
        get => (string?)GetValue(ProblemProperty);
        set => SetValue(ProblemProperty, value);
    }

    public ProductRow()
    {
        InitializeComponent();
    }

    public string? ProductName
    {
        get => (string?)GetValue(ProductNameProperty);
        set => SetValue(ProductNameProperty, value);
    }

    public string? Code
    {
        get => (string?)GetValue(CodeProperty);
        set => SetValue(CodeProperty, value);
    }

    public IReadOnlyList<TextRange>? NameRanges
    {
        get => (IReadOnlyList<TextRange>?)GetValue(NameRangesProperty);
        set => SetValue(NameRangesProperty, value);
    }

    public IReadOnlyList<TextRange>? CodeRanges
    {
        get => (IReadOnlyList<TextRange>?)GetValue(CodeRangesProperty);
        set => SetValue(CodeRangesProperty, value);
    }

    public bool IsOutOfStock
    {
        get => (bool)GetValue(IsOutOfStockProperty);
        set => SetValue(IsOutOfStockProperty, value);
    }

    public string? Note
    {
        get => (string?)GetValue(NoteProperty);
        set => SetValue(NoteProperty, value);
    }

    // The right column; it inherits the row's BindingContext
    public View? Right
    {
        get => RightHost.Content;
        set => RightHost.Content = value;
    }

    private static BindableProperty Property(string name, Type type, Action<ProductRow, object?> changed) =>
        BindableProperty.Create(name, type, typeof(ProductRow), type == typeof(bool) ? false : null,
            propertyChanged: (row, _, value) => changed((ProductRow)row, value));
}
