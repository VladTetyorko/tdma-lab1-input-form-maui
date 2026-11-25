using Mobile_apps_L1_Model_view.Services.Search;

namespace Mobile_apps_L1_Model_view.Views.Components;

// A label that marks the parts a search matched: they are semibold and in the accent colour (weight and colour,
// so colour is never the only signal), the rest is regular. Without ranges it is an ordinary label of its style
public sealed class HighlightedLabel : Label
{
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(string),
        typeof(HighlightedLabel), propertyChanged: (label, _, _) => ((HighlightedLabel)label).Show());

    public static readonly BindableProperty RangesProperty = BindableProperty.Create(nameof(Ranges),
        typeof(IReadOnlyList<TextRange>), typeof(HighlightedLabel),
        propertyChanged: (label, _, _) => ((HighlightedLabel)label).Show());

    // A row that cannot be chosen well (out of stock when issuing): everything in the secondary colour
    public static readonly BindableProperty IsDimmedProperty = BindableProperty.Create(nameof(IsDimmed), typeof(bool),
        typeof(HighlightedLabel), false, propertyChanged: (label, _, _) => ((HighlightedLabel)label).Show());

    public bool IsDimmed
    {
        get => (bool)GetValue(IsDimmedProperty);
        set => SetValue(IsDimmedProperty, value);
    }

    public HighlightedLabel()
    {
        SetDynamicResource(TextColorProperty, "TextPrimary");
    }

    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public IReadOnlyList<TextRange>? Ranges
    {
        get => (IReadOnlyList<TextRange>?)GetValue(RangesProperty);
        set => SetValue(RangesProperty, value);
    }

    private void Show()
    {
        var text = Value ?? string.Empty;
        if (IsDimmed)
        {
            SetDynamicResource(TextColorProperty, "TextSecondary");
        }

        if (Ranges is not { Count: > 0 } ranges)
        {
            FormattedText = null;
            Text = text;
            return;
        }

        var formatted = new FormattedString();
        var done = 0;
        foreach (var range in ranges.Where(range => range.End <= text.Length))
        {
            Add(formatted, text[done..range.Start], matched: false);
            Add(formatted, text.Substring(range.Start, range.Length), matched: true);
            done = range.End;
        }

        Add(formatted, text[done..], matched: false);
        FormattedText = formatted;
    }

    private void Add(FormattedString formatted, string text, bool matched)
    {
        if (text.Length == 0)
        {
            return;
        }

        var span = new Span { Text = text, FontFamily = matched ? "OpenSansSemibold" : "OpenSansRegular" };
        // A span does not take the colour of its label's style
        span.SetDynamicResource(Span.TextColorProperty,
            matched ? "PrimaryText" : IsDimmed ? "TextSecondary" : "TextPrimary");

        formatted.Spans.Add(span);
    }
}
