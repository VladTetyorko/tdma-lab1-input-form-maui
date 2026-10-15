using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// Pure sorting of product rows, so the rules are testable without a ViewModel.
// Text compares ignoring case, numbers compare as numbers (9 before 10), and products without
// a (valid) value for the chosen characteristic always come last, in either direction.
// Ties are broken by name, so the order is stable.
public static class ProductSorter
{
    public static IReadOnlyList<ProductSummary> Sort(
        IEnumerable<ProductSummary> products, SortOption option, bool descending) =>
        option.Kind switch
        {
            SortKind.Name => Order(products, p => p.Name, StringComparer.OrdinalIgnoreCase, descending),
            SortKind.Quantity => Order(products, p => p.Quantity, Comparer<int>.Default, descending),
            _ => SortByCharacteristic(products, option, descending)
        };

    private static List<ProductSummary> SortByCharacteristic(
        IEnumerable<ProductSummary> products, SortOption option, bool descending)
    {
        var list = products.ToList();
        var with = list.Where(p => ValueOf(p, option) is not null).ToList();
        var without = list.Except(with);

        var sorted = option.ValueType == CharacteristicValueType.Number
            ? Order(with, p => NumberOf(p, option), Comparer<double>.Default, descending)
            : Order(with, p => ValueOf(p, option)!, StringComparer.OrdinalIgnoreCase, descending);

        return [.. sorted, .. Order(without, p => p.Name, StringComparer.OrdinalIgnoreCase, false)];
    }

    // Null when the product has no usable value (absent, blank, or not a number for Number)
    private static string? ValueOf(ProductSummary product, SortOption option)
    {
        if (!product.Values.TryGetValue(option.CharacteristicId, out var text) || string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return option.ValueType == CharacteristicValueType.Number && !CharacteristicValue.TryParseNumber(text, out _)
            ? null
            : text;
    }

    private static double NumberOf(ProductSummary product, SortOption option)
    {
        CharacteristicValue.TryParseNumber(ValueOf(product, option), out var number);
        return number;
    }

    private static List<ProductSummary> Order<TKey>(
        IEnumerable<ProductSummary> items, Func<ProductSummary, TKey> key, IComparer<TKey> comparer, bool descending)
    {
        var ordered = descending ? items.OrderByDescending(key, comparer) : items.OrderBy(key, comparer);
        return [.. ordered.ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)];
    }
}
