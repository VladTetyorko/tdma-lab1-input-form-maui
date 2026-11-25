using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels.Sheets;

// What the user chose to do next in the sheet of one operation
public enum OperationChoice
{
    None,
    OpenProduct,
    ShowOnMap
}

// One operation in full: who, when, which document, from or to whom (shown only here) and why
public sealed partial class OperationSheetViewModel : SheetViewModel
{
    public OperationSheetViewModel(OperationEntry entry, DateTime nowUtc)
    {
        Entry = entry;
        var isIn = entry.Direction == OperationDirection.In;
        Title = $"{(isIn ? "+" : "−")}{entry.Quantity} {entry.ProductName}";
        ByText = $"{(isIn ? "Received" : "Issued")} by {entry.UserName} · {ActivityTime.Format(entry.At, nowUtc)}";
        var who = string.IsNullOrWhiteSpace(entry.Counterparty) ? null : $"{(isIn ? "from" : "to")} {entry.Counterparty}";
        DocumentText = string.Join(" · ", new[] { entry.DocumentNumber, who, entry.Note }
            .Where(text => !string.IsNullOrWhiteSpace(text)));
        LocationText = entry.Location is null ? string.Empty : $"Location {entry.Location}";
        SecondaryText = "Open product";
    }

    public OperationEntry Entry { get; }

    public string ByText { get; }

    public string DocumentText { get; }

    public bool HasDocumentText => DocumentText.Length > 0;

    public string LocationText { get; }

    public bool HasLocation => Entry.Location is not null;

    public OperationChoice Choice { get; private set; }

    protected override void OnSecondary() => Choose(OperationChoice.OpenProduct);

    [RelayCommand]
    private void ShowOnMap() => Choose(OperationChoice.ShowOnMap);

    private void Choose(OperationChoice choice)
    {
        Choice = choice;
        Close(true);
    }
}
