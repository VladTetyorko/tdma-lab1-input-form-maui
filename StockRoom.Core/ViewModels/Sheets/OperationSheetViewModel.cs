using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels.Sheets;

// What the user chose to do next in the sheet of one operation
public enum OperationChoice
{
    None,
    OpenProduct,
    ShowOnMap,
    Corrected
}

// One operation in full: who, when, which document, from or to whom (shown only here) and why. Its second step
// corrects the quantity: the user types what it should have been and the sheet says what will be recorded
public sealed partial class OperationSheetViewModel : SheetViewModel
{
    private const string CorrectText = "Correct quantity";

    private readonly OperationCorrector _corrector;
    private readonly string _summaryTitle;
    private Correction? _correction;

    public OperationSheetViewModel(OperationEntry entry, DateTime nowUtc, OperationCorrector corrector, bool canCorrect)
    {
        _corrector = corrector;
        Entry = entry;
        var isIn = entry.Direction == OperationDirection.In;
        Title = _summaryTitle = $"{(isIn ? "+" : "−")}{entry.Quantity} {entry.ProductName}";
        ByText = $"{(isIn ? "Received" : "Issued")} by {entry.UserName} · {ActivityTime.Format(entry.At, nowUtc)}";
        var who = string.IsNullOrWhiteSpace(entry.Counterparty) ? null : $"{(isIn ? "from" : "to")} {entry.Counterparty}";
        DocumentText = string.Join(" · ", new[] { entry.DocumentNumber, who, entry.Note }
            .Where(text => !string.IsNullOrWhiteSpace(text)));
        LocationText = entry.Location is null ? string.Empty : $"Location {entry.Location}";
        SecondaryText = "Open product";
        PrimaryText = canCorrect ? CorrectText : string.Empty;
    }

    public OperationEntry Entry { get; }

    public string ByText { get; }

    public string DocumentText { get; }

    public bool HasDocumentText => DocumentText.Length > 0;

    public string LocationText { get; }

    public bool HasLocation => Entry.Location is not null;

    public OperationChoice Choice { get; private set; }

    // "Received 9 units · R-..." after a correction
    public string Result { get; private set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSummary))]
    public partial bool IsCorrecting { get; set; }

    public bool IsSummary => !IsCorrecting;

    // What the quantity should have been, as typed
    [ObservableProperty]
    public partial string RightQuantity { get; set; } = string.Empty;

    // "Issues 9 units. Note: Correction of R-…"
    [ObservableProperty]
    public partial string Preview { get; set; } = string.Empty;

    protected override bool CanAccept => !IsCorrecting || _correction is { Refusal: null };

    // The preview of what was typed last (tests wait for it)
    public Task Previewing { get; private set; } = Task.CompletedTask;

    partial void OnRightQuantityChanged(string value)
    {
        if (IsCorrecting)
        {
            Previewing = PreviewAsync(value);
        }
    }

    protected override async Task<bool> OnPrimaryAsync()
    {
        if (!IsCorrecting)
        {
            var booked = await _corrector.GetBookedAsync(Entry);
            IsCorrecting = true;
            StepText = "Step 2 of 2";
            Title = $"Correct {Entry.ProductName}";
            SecondaryText = "Back";
            RightQuantity = booked.ToString();
            await Previewing;
            return false;
        }

        var result = await _corrector.ApplyAsync(Entry, int.Parse(RightQuantity));
        if (!result.IsSuccess)
        {
            Hint = result.Error!;
            return false;
        }

        Result = $"{_correction!.Action.Replace("Receive", "Received").Replace("Issue", "Issued")} · {result.Value!.Number}";
        Choice = OperationChoice.Corrected;
        return true;
    }

    // Back from step 2 to the summary; from the summary the same button opens the product
    protected override void OnSecondary()
    {
        if (!IsCorrecting)
        {
            Choose(OperationChoice.OpenProduct);
            return;
        }

        IsCorrecting = false;
        StepText = string.Empty;
        Title = _summaryTitle;
        SecondaryText = "Open product";
        PrimaryText = CorrectText;
        Hint = string.Empty;
        RefreshPrimary();
    }

    [RelayCommand]
    private void ShowOnMap() => Choose(OperationChoice.ShowOnMap);

    private async Task PreviewAsync(string typed)
    {
        var correction = int.TryParse(typed, out var right) ? await _corrector.PreviewAsync(Entry, right) : null;

        // Something else was typed meanwhile
        if (typed != RightQuantity)
        {
            return;
        }

        _correction = correction;
        PrimaryText = correction?.Action ?? CorrectText;
        Preview = correction is null
            ? "Type what the quantity should have been."
            : $"{(correction.Direction == OperationDirection.In ? "Receives" : "Issues")} {Plural.Units(correction.Quantity)}. Note: {correction.Note}";
        Hint = correction?.Refusal ?? string.Empty;
        RefreshPrimary();
    }

    private void Choose(OperationChoice choice)
    {
        Choice = choice;
        Close(true);
    }
}
