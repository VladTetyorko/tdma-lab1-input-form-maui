namespace Mobile_apps_L1_Model_view.Views.Components;

// A table container: fixed-width columns plus one stretching column. When the viewport is wider than MinimumTableWidth
// the content fills it; when it is narrower the content keeps MinimumTableWidth and scrolls sideways (header and rows
// together, because both are inside the content)
public sealed class WideTable : ScrollView
{
    public static readonly BindableProperty MinimumTableWidthProperty = BindableProperty.Create(
        nameof(MinimumTableWidth), typeof(double), typeof(WideTable), 0d,
        propertyChanged: (table, _, _) => ((WideTable)table).FitContent(((WideTable)table).Width));

    public WideTable()
    {
        Orientation = ScrollOrientation.Horizontal;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Default;
    }

    public double MinimumTableWidth
    {
        get => (double)GetValue(MinimumTableWidthProperty);
        set => SetValue(MinimumTableWidthProperty, value);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        FitContent(width);
    }

    // Only when the value changes, so the layout does not loop
    private void FitContent(double viewportWidth)
    {
        if (Content is not View content || viewportWidth <= 0)
        {
            return;
        }

        var wanted = Math.Max(viewportWidth, MinimumTableWidth);
        if (Math.Abs(content.WidthRequest - wanted) > 0.5)
        {
            content.WidthRequest = wanted;
        }
    }
}
