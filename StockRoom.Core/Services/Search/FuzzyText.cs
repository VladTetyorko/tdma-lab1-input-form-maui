namespace Mobile_apps_L1_Model_view.Services.Search;

// The two typo-tolerant comparisons of the product search
public static class FuzzyText
{
    // Which of the distinct character n-grams of the token occur in the word, as a share, and where in the word.
    // One space in front of both makes the first letters count
    public static (double Share, IReadOnlyList<TextRange> Spans) Overlap(string token, string word)
    {
        var size = token.Length < 5 ? 2 : 3;
        var padded = " " + token;
        var target = " " + word;
        var grams = Enumerable.Range(0, padded.Length - size + 1).Select(i => padded.Substring(i, size)).Distinct().ToList();
        var found = 0;
        var spans = new List<TextRange>();
        foreach (var gram in grams)
        {
            var at = target.IndexOf(gram, StringComparison.Ordinal);
            if (at < 0)
            {
                continue;
            }

            found++;
            var start = Math.Max(0, at - 1);
            spans.Add(new TextRange(start, at + size - 1 - start));
        }

        return ((double)found / grams.Count, spans);
    }

    // Damerau-Levenshtein (optimal string alignment): insert, delete, replace and swap of neighbours cost one
    public static int Distance(string a, string b)
    {
        var table = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++)
        {
            table[i, 0] = i;
        }

        for (var j = 0; j <= b.Length; j++)
        {
            table[0, j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                table[i, j] = Math.Min(Math.Min(table[i - 1, j] + 1, table[i, j - 1] + 1), table[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                {
                    table[i, j] = Math.Min(table[i, j], table[i - 2, j - 2] + 1);
                }
            }
        }

        return table[a.Length, b.Length];
    }
}
