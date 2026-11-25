using System.Globalization;
using System.Text;

namespace Mobile_apps_L1_Model_view.Services.Search;

// A text prepared for comparing: lower case, no diacritics, every run of other characters is one space.
// Map keeps, for every normalised character, its position in the original, so a match can be highlighted there
public sealed class SearchText
{
    private readonly int[] _map;

    private SearchText(string value, int[] map)
    {
        Value = value;
        _map = map;
    }

    public string Value { get; }

    public static SearchText From(string? original)
    {
        var text = new StringBuilder();
        var map = new List<int>();
        var source = original ?? string.Empty;
        for (var i = 0; i < source.Length; i++)
        {
            foreach (var part in source[i].ToString().Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(part) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(part))
                {
                    text.Append(char.ToLowerInvariant(part));
                    map.Add(i);
                }
                else if (text.Length > 0 && text[^1] != ' ')
                {
                    text.Append(' ');
                    map.Add(i);
                }
            }
        }

        if (text.Length > 0 && text[^1] == ' ')
        {
            text.Length--;
            map.RemoveAt(map.Count - 1);
        }

        return new SearchText(text.ToString(), [.. map]);
    }

    // The words with the position of their first letter in Value
    public IEnumerable<(string Word, int Start)> Words()
    {
        var start = 0;
        foreach (var word in Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            start = Value.IndexOf(word, start, StringComparison.Ordinal);
            yield return (word, start);
            start += word.Length;
        }
    }

    // A range of Value as a range of the original text
    public TextRange Original(int start, int length)
    {
        var first = _map[start];
        return new TextRange(first, _map[start + length - 1] + 1 - first);
    }
}
