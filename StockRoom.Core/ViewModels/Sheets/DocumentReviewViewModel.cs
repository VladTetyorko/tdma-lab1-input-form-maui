using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels.Sheets;

// A supplier or recipient used before, offered as a chip. The name is personal data: shown only, never logged
public sealed partial class NameChip(string name) : ObservableObject
{
    public string Name { get; } = name;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

// The review before a receipt or issue is committed: what will move, from or to whom (the last three names as chips),
// an optional note. The main action records it; the sheet cannot be closed while it saves
public sealed partial class DocumentReviewViewModel : SheetViewModel
{
    public const int ShownLines = 5;

    private readonly Func<string, string, Task> _commit;

    public DocumentReviewViewModel(
        OperationDirection direction,
        IReadOnlyList<StockDocumentLineItem> lines,
        IReadOnlyList<string> recentNames,
        string counterparty,
        string note,
        Func<string, string, Task> commit)
    {
        _commit = commit;
        var issuing = direction == OperationDirection.Out;
        var units = lines.Sum(line => line.Quantity);
        Title = $"{(issuing ? "Issue" : "Receive")} {Plural.Units(units)}";
        PrimaryText = Title;
        SecondaryText = "Back";
        NameLabel = issuing ? "To" : "From";
        NamePlaceholder = issuing ? "Recipient (optional)" : "Supplier (optional)";
        Lines = [.. lines.Take(ShownLines)];
        MoreText = lines.Count > ShownLines ? $"+ {lines.Count - ShownLines} more" : string.Empty;
        foreach (var name in recentNames)
        {
            Names.Add(new NameChip(name));
        }

        Counterparty = counterparty;
        Note = note;
        // The field is shown when there is nothing to pick from, or what the draft holds is not one of the chips
        IsTypingName = Names.Count == 0 || (counterparty.Length > 0 && Names.All(chip => chip.Name != counterparty));
        IsNoteOpen = note.Length > 0;
        Mark();
    }

    public IReadOnlyList<StockDocumentLineItem> Lines { get; }

    public string MoreText { get; }

    public bool HasMore => MoreText.Length > 0;

    public string NameLabel { get; }

    public string NamePlaceholder { get; }

    public ObservableCollection<NameChip> Names { get; } = [];

    public bool HasNames => Names.Count > 0;

    [ObservableProperty]
    public partial string Counterparty { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsTypingName { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoteSummary))]
    public partial string Note { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsNoteOpen { get; set; }

    public string NoteSummary => Note.Length == 0 ? "none" : Note;

    partial void OnCounterpartyChanged(string value) => Mark();

    // A second tap on the chosen name clears it
    [RelayCommand]
    private void PickName(NameChip? chip)
    {
        if (chip is null)
        {
            return;
        }

        IsTypingName = false;
        Counterparty = chip.IsSelected ? string.Empty : chip.Name;
    }

    [RelayCommand]
    private void NewName()
    {
        IsTypingName = true;
        Counterparty = string.Empty;
    }

    [RelayCommand]
    private void ToggleNote() => IsNoteOpen = !IsNoteOpen;

    protected override async Task<bool> OnPrimaryAsync()
    {
        await _commit(Counterparty, Note);
        return true;
    }

    private void Mark()
    {
        foreach (var chip in Names)
        {
            chip.IsSelected = chip.Name == Counterparty;
        }
    }
}
