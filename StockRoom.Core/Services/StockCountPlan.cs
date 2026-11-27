namespace Mobile_apps_L1_Model_view.Services;

// One counted product against the database as it is now
public sealed record StockCountLine(ProductSummary Product, int OnHand, int Counted, bool ChangedSinceCounted)
{
    public int Difference => Counted - OnHand;
}

// What posting a count would do. Lines that changed since they were counted are kept out until they are counted again
public sealed record StockCountPlan(IReadOnlyList<StockCountLine> Lines, int NotCounted)
{
    public IReadOnlyList<StockCountLine> Corrections =>
        [.. Lines.Where(line => !line.ChangedSinceCounted && line.Difference != 0)];

    public IReadOnlyList<StockCountLine> Recounts => [.. Lines.Where(line => line.ChangedSinceCounted)];

    public int Matching => Lines.Count(line => !line.ChangedSinceCounted && line.Difference == 0);
}
