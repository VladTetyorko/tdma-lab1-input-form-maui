using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// The receipt or issue being prepared: the line rules, without storage or UI. The newest touched line comes first
public sealed class StockDocumentDraft(OperationDirection direction)
{
    private readonly List<DraftLine> _lines = [];

    public OperationDirection Direction { get; } = direction;

    // Supplier or recipient (personal data: never logged)
    public string Counterparty { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public IReadOnlyList<DraftLine> Lines => _lines;

    public int TotalUnits => _lines.Sum(line => line.Quantity);

    public bool IsEmpty => _lines.Count == 0 && Counterparty.Length == 0 && Note.Length == 0;

    // The same barcode again makes its line one bigger instead of adding a second line
    public void Add(string barcode, int quantity = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(barcode);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        var index = IndexOf(barcode);
        if (index >= 0)
        {
            quantity += _lines[index].Quantity;
            _lines.RemoveAt(index);
        }

        _lines.Insert(0, new DraftLine(barcode, quantity));
    }

    // Zero or less removes the line; an unknown barcode changes nothing
    public void SetQuantity(string barcode, int quantity)
    {
        var index = IndexOf(barcode);
        if (index < 0)
        {
            return;
        }

        if (quantity <= 0)
        {
            _lines.RemoveAt(index);
        }
        else
        {
            _lines[index] = new DraftLine(barcode, quantity);
        }
    }

    // What was removed and where it stood, for undo; null when there is no such line
    public (DraftLine Line, int Index)? Remove(string barcode)
    {
        var index = IndexOf(barcode);
        if (index < 0)
        {
            return null;
        }

        var line = _lines[index];
        _lines.RemoveAt(index);
        return (line, index);
    }

    // Undo of Remove; if the barcode was scanned again meanwhile the units are added to that line
    public void Restore(DraftLine line, int index)
    {
        if (IndexOf(line.Barcode) >= 0)
        {
            Add(line.Barcode, line.Quantity);
        }
        else
        {
            _lines.Insert(Math.Clamp(index, 0, _lines.Count), line);
        }
    }

    public void Clear()
    {
        _lines.Clear();
        Counterparty = string.Empty;
        Note = string.Empty;
    }

    private int IndexOf(string barcode) => _lines.FindIndex(line => line.Barcode == barcode);
}
