namespace Mobile_apps_L1_Model_view.Services.Search;

// A part of a text, as positions in the original string
public readonly record struct TextRange(int Start, int Length)
{
    public int End => Start + Length;
}
