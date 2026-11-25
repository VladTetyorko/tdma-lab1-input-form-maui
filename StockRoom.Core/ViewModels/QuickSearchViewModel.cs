using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// The quick-search drop-down: type a name or code, pick a product. Enter keeps keyboard-wedge scanners working
// (see SubmitAsync). The host page decides what picking and submitting mean
public sealed partial class QuickSearchViewModel(
    IProductCatalog catalog,
    ICharacteristicService characteristics,
    ICurrentUserService currentUser,
    IPermissionGuard guard,
    INavigationService navigation,
    ILogger<QuickSearchViewModel> logger) : ObservableObject
{
    private IReadOnlyList<ProductSummary> _products = [];
    private IReadOnlyList<Characteristic> _characteristics = [];

    // A suggestion was chosen
    public event EventHandler<ProductSummary>? Picked;

    // Enter on a barcode that no highlighted row stands for (also an unknown barcode)
    public event EventHandler<string>? SubmittedText;

    // The product form is about to open for something that was not found
    public event EventHandler? CreateRequested;

    // At most so many rows: 5 on a phone, 8 on a desktop (set by the view)
    public int MaxSuggestions { get; set; } = 5;

    // The text is the field's own value (the product form's name): picking keeps it, and "no match" is good news
    public bool IsSelectMode { get; set; }

    // The product being edited is not suggested to itself
    public Guid? ExcludedProductId { get; set; }

    // What was loaded last
    public IReadOnlyList<ProductSummary> Products => _products;

    [ObservableProperty]
    public partial string Query { get; set; } = string.Empty;

    public ObservableCollection<QuickSearchRow> Suggestions { get; } = [];

    // -1 = none
    [ObservableProperty]
    public partial int HighlightedIndex { get; set; } = -1;

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    [ObservableProperty]
    public partial bool HasSuggestions { get; set; }

    // Not empty when the text matches nothing
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoMatch))]
    public partial string NoMatchText { get; set; } = string.Empty;

    public bool HasNoMatch => NoMatchText.Length > 0;

    // "Create product ..." is offered (nothing matches and the user may add products)
    [ObservableProperty]
    public partial bool CanCreate { get; set; }

    [ObservableProperty]
    public partial string CreateText { get; set; } = string.Empty;

    // Reads the products to search in; the page calls it every time it appears and after a commit
    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            _products = await catalog.GetSummariesAsync();
            _characteristics = await characteristics.GetAllAsync();
        }
        catch (Exception ex)
        {
            // Suggestions are a help: the page keeps working without them
            logger.LogWarning(ex, "The products for the quick search could not be read");
        }

        if (IsOpen)
        {
            Search();
        }
    }

    partial void OnQueryChanged(string value) => Search();

    partial void OnHighlightedIndexChanged(int value)
    {
        for (var i = 0; i < Suggestions.Count; i++)
        {
            Suggestions[i].IsHighlighted = i == value;
        }
    }

    [RelayCommand]
    private void Pick(QuickSearchRow? row)
    {
        if (row is null)
        {
            return;
        }

        var product = row.Suggestion.Product;
        if (IsSelectMode)
        {
            Close();
        }
        else
        {
            Query = string.Empty;
        }

        Picked?.Invoke(this, product);
    }

    [RelayCommand]
    private void MoveNext() => Move(1);

    [RelayCommand]
    private void MovePrevious() => Move(-1);

    // A highlighted row wins; then a barcode goes the exact-barcode way; then a single suggestion. Else nothing
    [RelayCommand]
    private void Submit()
    {
        if (IsOpen && HighlightedIndex >= 0 && HighlightedIndex < Suggestions.Count)
        {
            Pick(Suggestions[HighlightedIndex]);
        }
        else if (ScanPayloadParser.Parse(Query).IsSuccess)
        {
            IsOpen = false;
            SubmittedText?.Invoke(this, Query);
        }
        else if (IsOpen && Suggestions.Count == 1)
        {
            Pick(Suggestions[0]);
        }
    }

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
        HighlightedIndex = -1;
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        var text = Query.Trim();
        if (text.Length == 0 || await guard.CheckAsync(RolePermissions.CanManageProducts, "add products") is not null)
        {
            return;
        }

        // Digits are taken as the barcode, anything else as the name
        var key = text.All(char.IsAsciiDigit) ? NavigationKeys.Barcode : NavigationKeys.ProductName;
        logger.LogInformation("Opening the product form from the quick search");
        Query = string.Empty;
        CreateRequested?.Invoke(this, EventArgs.Empty);
        await navigation.GoToAsync(Routes.ProductForm, new Dictionary<string, object> { [key] = text });
    }

    // Arrows wrap around the ends
    private void Move(int step)
    {
        if (!IsOpen || Suggestions.Count == 0)
        {
            return;
        }

        var next = HighlightedIndex < 0 ? (step > 0 ? 0 : Suggestions.Count - 1) : HighlightedIndex + step;
        HighlightedIndex = (next + Suggestions.Count) % Suggestions.Count;
    }

    private void Search()
    {
        HighlightedIndex = -1;
        Suggestions.Clear();
        var searched = _products.Where(product => product.Id != ExcludedProductId);
        foreach (var suggestion in ProductSuggestions.Find(searched, _characteristics, Query, MaxSuggestions))
        {
            Suggestions.Add(new QuickSearchRow(suggestion));
        }

        HasSuggestions = Suggestions.Count > 0;
        IsOpen = ProductSuggestions.IsSearchable(Query) && (HasSuggestions || !IsSelectMode);
        var nothing = IsOpen && !HasSuggestions;
        var mayCreate = currentUser.CurrentUser is { } user && RolePermissions.CanManageProducts(user.Role);
        var text = Query.Trim();
        NoMatchText = !nothing ? string.Empty
            : mayCreate ? $"No product matches \"{text}\"."
            : $"No product matches \"{text}\". Ask a manager to add it.";
        CanCreate = nothing && mayCreate;
        CreateText = CanCreate ? $"Create product \"{text}\"" : string.Empty;
        logger.LogDebug("Quick search found {Count} products", Suggestions.Count);
    }
}
