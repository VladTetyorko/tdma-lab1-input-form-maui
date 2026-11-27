using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels.Sheets;

// "Nutella 400 g   36 → 38   +2"
public sealed record CountReviewLine(string Name, string Barcode, string Change, string Difference, bool IsMissing, string Problem);

// What posting the stock count would record, before it is recorded
public sealed class CountReviewViewModel : SheetViewModel
{
    private readonly Func<Task> _post;

    public CountReviewViewModel(StockCountPlan plan, string note, Func<Task> post)
    {
        _post = post;
        Title = "Post stock count corrections";
        var corrections = plan.Corrections;
        PrimaryText = corrections.Count switch
        {
            0 => "Finish count",
            1 => "Post 1 correction",
            _ => $"Post {corrections.Count} corrections"
        };
        SecondaryText = "Back";
        Lines =
        [
            .. corrections.Select(line => new CountReviewLine(line.Product.Name, line.Product.Barcode,
                $"{line.OnHand} → {line.Counted}",
                line.Difference > 0 ? $"+{line.Difference}" : $"−{-line.Difference}", line.Difference < 0, string.Empty)),
            .. plan.Recounts.Select(line => new CountReviewLine(line.Product.Name, line.Product.Barcode,
                $"counted {line.Counted}, on hand now {line.OnHand}", string.Empty, false, StockCountViewModel.RecountText))
        ];
        Summary = string.Join(" ", new[]
        {
            plan.Matching == 0 ? null : $"{Plural.Products(plan.Matching)} {(plan.Matching == 1 ? "matches" : "match")}.",
            plan.NotCounted == 0 ? null : $"{Plural.Products(plan.NotCounted)} not counted: unchanged."
        }.Where(text => text is not null));
        NoteText = corrections.Count == 0 ? string.Empty : $"Note: {note}";
    }

    public IReadOnlyList<CountReviewLine> Lines { get; }

    public string Summary { get; }

    public string NoteText { get; }

    protected override async Task<bool> OnPrimaryAsync()
    {
        await _post();
        return true;
    }
}
