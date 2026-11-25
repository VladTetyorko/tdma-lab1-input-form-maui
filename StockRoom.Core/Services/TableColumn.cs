namespace Mobile_apps_L1_Model_view.Services;

// One column header of the product table. Sort is null for a column that cannot be sorted by
public sealed record TableColumn(string Title, SortOption? Sort, double Width, bool IsNumeric = false)
{
    public bool IsSorted { get; init; }

    public bool IsDescending { get; init; }

    public bool IsAscending => IsSorted && !IsDescending;

    public bool IsSortedDescending => IsSorted && IsDescending;

    public string AutomationId => Sort is null ? $"Header_{Title}" : $"SortHeader_{Sort.Title}";

    // The header text with the direction arrow, so the state never depends on colour alone. A numeric column is
    // aligned right, so its arrow goes first and the right edges of header and numbers line up
    public string Header => !IsSorted ? Title : IsNumeric ? $"{Arrow} {Title}" : $"{Title} {Arrow}";

    private string Arrow => IsDescending ? "▼" : "▲";

    // What a screen reader announces
    public string Description => Sort is null
        ? Title
        : IsSorted
            ? $"{Title}, sorted {(IsDescending ? "descending" : "ascending")}. Activate to sort {(IsDescending ? "ascending" : "descending")}."
            : $"{Title}. Activate to sort ascending.";
}
