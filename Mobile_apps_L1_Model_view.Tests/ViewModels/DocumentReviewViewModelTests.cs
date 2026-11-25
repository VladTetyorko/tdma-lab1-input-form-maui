using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels;
using Mobile_apps_L1_Model_view.ViewModels.Sheets;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class DocumentReviewViewModelTests
{
    private readonly List<(string Name, string Note)> _commits = [];

    private DocumentReviewViewModel Review(
        int lines = 2, string[]? names = null, string counterparty = "", string note = "",
        OperationDirection direction = OperationDirection.In) =>
        new(direction,
            [.. Enumerable.Range(1, lines).Select(i => new StockDocumentLineItem(
                $"code{i}", new ProductSummary(Guid.NewGuid(), $"Product {i}", $"code{i}", 10), i, direction))],
            names ?? ["Metro", "Ferrero"], counterparty, note,
            (name, text) =>
            {
                _commits.Add((name, text));
                return Task.CompletedTask;
            });

    [Fact]
    public void TheMainAction_IsNamedAfterItsResult()
    {
        Assert.Equal(("Receive 3 units", "Back", "From"), (Review().PrimaryText, Review().SecondaryText, Review().NameLabel));
        Assert.Equal(("Issue 3 units", "To"),
            (Review(direction: OperationDirection.Out).PrimaryText, Review(direction: OperationDirection.Out).NameLabel));
    }

    [Fact]
    public void ManyLines_AreCut()
    {
        var review = Review(lines: 7);

        Assert.Equal(5, review.Lines.Count);
        Assert.Equal("+ 2 more", review.MoreText);
    }

    [Fact]
    public void Chips_PickAndUnpickAName()
    {
        var review = Review();
        Assert.False(review.IsTypingName);

        review.PickNameCommand.Execute(review.Names[0]);
        Assert.Equal("Metro", review.Counterparty);
        Assert.True(review.Names[0].IsSelected);

        review.PickNameCommand.Execute(review.Names[0]);
        Assert.Equal(string.Empty, review.Counterparty);

        review.NewNameCommand.Execute(null);
        Assert.True(review.IsTypingName);
    }

    [Fact]
    public void WithoutRecentNames_OrWithAnotherName_TheFieldIsShown()
    {
        Assert.True(Review(names: []).IsTypingName);
        Assert.True(Review(counterparty: "Someone").IsTypingName);
        Assert.False(Review(counterparty: "Metro").IsTypingName);
    }

    [Fact]
    public async Task Accept_CommitsOnceWithTheNameAndNote()
    {
        var review = Review(note: "W-1");
        review.PickNameCommand.Execute(review.Names[1]);

        await review.PrimaryCommand.ExecuteAsync(null);
        review.Dismiss();

        Assert.Equal(("Ferrero", "W-1"), Assert.Single(_commits));
        Assert.True(review.Accepted);
    }

    [Fact]
    public async Task WhileSaving_TheSheetCannotBeClosed()
    {
        var saving = new TaskCompletionSource();
        var review = new DocumentReviewViewModel(OperationDirection.In, [], [], string.Empty, string.Empty,
            (_, _) => saving.Task);
        var closed = false;
        review.Closed += (_, _) => closed = true;

        var accept = review.PrimaryCommand.ExecuteAsync(null);
        review.Dismiss();

        Assert.False(closed);
        Assert.False(review.SecondaryCommand.CanExecute(null));
        saving.SetResult();
        await accept;
        Assert.True(closed && review.Accepted);
    }
}
