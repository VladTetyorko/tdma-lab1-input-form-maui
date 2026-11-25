namespace Mobile_apps_L1_Model_view.Services.Search;

// The one rule of both product searches (quick search and table filter), pure and without I/O.
// Every word of the query must match (AND); the score is the sum of the best tier of each word
public static class ProductMatcher
{
    private const double MinimumShare = 0.6;

    // Best first; at equal score exact before similar, then (when asked) products in stock, then by name
    public static IReadOnlyList<MatchResult> Rank(
        IEnumerable<ProductSummary> products, string? text, bool inStockFirst = false, bool includeValues = false)
    {
        var query = SearchQuery.Parse(text);
        return query.IsEmpty
            ? []
            :
            [
                .. products
                    .Select(product => Match(product, query, includeValues))
                    .OfType<MatchResult>()
                    .OrderByDescending(match => match.Score)
                    .ThenBy(match => match.IsFuzzy)
                    .ThenBy(match => inStockFirst && match.Product.Quantity <= 0)
                    .ThenBy(match => match.Product.Name, StringComparer.OrdinalIgnoreCase)
            ];
    }

    // Null = the product does not match
    public static MatchResult? Match(ProductSummary product, SearchQuery query, bool includeValues = false)
    {
        var name = SearchText.From(product.Name);
        var nameRanges = new List<TextRange>();
        var codeRanges = new List<TextRange>();
        double score = 0;
        var fuzzy = false;
        foreach (var token in query.Tokens)
        {
            var alone = query.Tokens.Count == 1;
            if (token.IsDigits && Code(product.Barcode, token.Text) is { } code)
            {
                score += code.Score;
                codeRanges.Add(code.Range);
            }
            else if (Exact(name, token.Text, alone) is { } exact)
            {
                score += exact.Score;
                nameRanges.Add(name.Original(exact.Range.Start, exact.Range.Length));
            }
            else if (includeValues && InValues(product, token.Text))
            {
                score += 200;
            }
            else if (!token.IsDigits && Similar(name, token.Text) is { } similar)
            {
                score += similar.Score;
                fuzzy = true;
                nameRanges.AddRange(similar.Ranges.Select(range => name.Original(range.Start, range.Length)));
            }
            else
            {
                return null;
            }
        }

        return new MatchResult(product, score, fuzzy, Merge(nameRanges), Merge(codeRanges));
    }

    // A code is right or it is not: equal, prefix, the last digits (3+), anywhere (4+)
    private static (double Score, TextRange Range)? Code(string barcode, string token)
    {
        if (barcode == token)
        {
            return (1000, new TextRange(0, token.Length));
        }

        if (barcode.StartsWith(token, StringComparison.Ordinal))
        {
            return (800, new TextRange(0, token.Length));
        }

        if (token.Length >= 3 && barcode.EndsWith(token, StringComparison.Ordinal))
        {
            return (700, new TextRange(barcode.Length - token.Length, token.Length));
        }

        var at = token.Length >= 4 ? barcode.IndexOf(token, StringComparison.Ordinal) : -1;
        return at >= 0 ? (500, new TextRange(at, token.Length)) : null;
    }

    // The name starts with it, a word starts with it (2+), the name contains it (3+). Ranges are in the normalised text
    private static (double Score, TextRange Range)? Exact(SearchText name, string token, bool alone)
    {
        if (name.Value.StartsWith(token, StringComparison.Ordinal) && !(token.All(char.IsAsciiDigit) && token.Length < 2))
        {
            return (600, new TextRange(0, token.Length));
        }

        foreach (var (word, start) in name.Words())
        {
            // A single letter next to other words ("500 g") may be a whole word
            if (token.Length >= 2 ? word.StartsWith(token, StringComparison.Ordinal) : !alone && word == token)
            {
                return (500, new TextRange(start, token.Length));
            }
        }

        var at = token.Length >= 3 ? name.Value.IndexOf(token, StringComparison.Ordinal) : -1;
        return at >= 0 ? (350, new TextRange(at, token.Length)) : null;
    }

    private static bool InValues(ProductSummary product, string token) =>
        product.Values.Values.Any(value => Exact(SearchText.From(value), token, alone: false) is not null);

    // Shared character n-grams (3+ letters) or a small edit distance to the start of a word (4+ letters)
    private static (double Score, IReadOnlyList<TextRange> Ranges)? Similar(SearchText name, string token)
    {
        (double Score, IReadOnlyList<TextRange> Ranges)? best = null;
        if (token.Length < 3)
        {
            return best;
        }

        var allowed = token.Length >= 8 ? 2 : 1;
        foreach (var (word, start) in name.Words())
        {
            var (share, spans) = FuzzyText.Overlap(token, word);
            if (share >= MinimumShare && 100 + 200 * share > (best?.Score ?? 0))
            {
                best = (100 + 200 * share, [.. spans.Select(span => span with { Start = span.Start + start })]);
            }

            if (token.Length < 4)
            {
                continue;
            }

            var prefix = word[..Math.Min(word.Length, token.Length)];
            var distance = FuzzyText.Distance(token, prefix);
            if (distance <= allowed && 150 - 40 * distance > (best?.Score ?? 0))
            {
                best = (150 - 40 * distance, [new TextRange(start, prefix.Length)]);
            }
        }

        return best;
    }

    // Overlapping, touching and one character apart ranges become one
    private static IReadOnlyList<TextRange> Merge(List<TextRange> ranges)
    {
        var merged = new List<TextRange>();
        foreach (var range in ranges.OrderBy(range => range.Start))
        {
            if (merged.Count > 0 && range.Start <= merged[^1].End + 1)
            {
                var last = merged[^1];
                merged[^1] = last with { Length = Math.Max(last.End, range.End) - last.Start };
            }
            else
            {
                merged.Add(range);
            }
        }

        return merged;
    }
}
