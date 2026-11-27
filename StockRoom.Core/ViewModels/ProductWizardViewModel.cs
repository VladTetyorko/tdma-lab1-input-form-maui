using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels.Sheets;

namespace Mobile_apps_L1_Model_view.ViewModels;

// Adding a product in three steps: the code, the name and photo, the characteristics we sort by. Everything about the
// product itself (camera, open databases, duplicate warning, photo, draft, saving, discarding) is ProductFormViewModel's;
// this class only walks through the steps and adds the characteristic values
public sealed partial class ProductWizardViewModel : ObservableObject, INavigationParameterReceiver, IPageLifecycleAware
{
    public const int LastStep = 3;

    private readonly ICharacteristicService _characteristics;
    private readonly ICharacteristicValidator _validator;
    private readonly ISheetService _sheets;

    // 2 when the code came with the navigation (an unknown line, an unknown code on Search)
    private int _firstStep = 1;

    public ProductWizardViewModel(
        ProductFormViewModel form,
        ICharacteristicService characteristics,
        ICharacteristicValidator validator,
        ISheetService sheets)
    {
        Form = form;
        _characteristics = characteristics;
        _validator = validator;
        _sheets = sheets;
        form.Saved = SaveValuesAsync;
        form.PropertyChanged += (_, _) => Refresh();
    }

    public ProductFormViewModel Form { get; }

    // The characteristics of step 3; all of them may stay empty
    public ObservableCollection<CharacteristicEditItem> Details { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCodeStep))]
    [NotifyPropertyChangedFor(nameof(IsNameStep))]
    [NotifyPropertyChangedFor(nameof(IsDetailsStep))]
    public partial int Step { get; set; } = 1;

    public bool IsCodeStep => Step == 1;

    public bool IsNameStep => Step == 2;

    public bool IsDetailsStep => Step == LastStep;

    public string StepText => $"Step {Step} of {LastStep} · {Step switch { 1 => "Code", 2 => "Name and photo", _ => "Details" }}";

    // "● ○ ○"
    public string Dots => string.Join(" ", Enumerable.Range(1, LastStep).Select(step => step <= Step ? "●" : "○"));

    // The main button is named after what comes next
    public string NextText => Step switch { 1 => "Next: name and photo", 2 => "Next: details", _ => "Add product" };

    // Why it is disabled, or empty
    public string Hint => Step switch
    {
        1 when Form.BarcodeError.Length > 0 => Form.BarcodeError,
        1 when Form.Barcode.Length == 0 => "Scan or type the barcode",
        1 when IsTaken => "This code is already in the catalogue",
        2 when Form.NameError.Length > 0 => Form.NameError,
        2 when Form.Name.Trim().Length == 0 => "Enter a name",
        LastStep when Details.Any(item => item.Error.Length > 0) => "Fix the red fields",
        _ => string.Empty
    };

    private bool IsTaken => Form.QuickSearch.Products.Any(product =>
        product.Barcode == (BarcodeNormalizer.ToEan13(Form.Barcode) ?? Form.Barcode.Trim()));

    private bool CanNext => Hint.Length == 0 && (Step != LastStep || Form.SaveCommand.CanExecute(null));

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Form.ApplyQueryAttributes(query);
        if (query.TryGetValue(NavigationKeys.Barcode, out var value) && value is string)
        {
            _firstStep = 2;
            Step = 2;
        }

        Refresh();
    }

    public void OnAppearing() => AppearCommand.Execute(null);

    public void OnDisappearing() => Form.OnDisappearing();

    [RelayCommand]
    private async Task AppearAsync()
    {
        await Form.AppearCommand.ExecuteAsync(null);
        // The camera belongs to the first step only
        if (Step != 1)
        {
            Form.OnDisappearing();
        }

        await LoadDetailsAsync();
        Refresh();
    }

    [RelayCommand(CanExecute = nameof(CanNext))]
    private async Task NextAsync()
    {
        if (Step == LastStep)
        {
            await Form.SaveCommand.ExecuteAsync(null);
            return;
        }

        if (Step == 1)
        {
            Form.NormalizeBarcode();
        }

        Step++;
        if (Step == 2)
        {
            Form.OnDisappearing();
        }
        else
        {
            Prefill();
        }

        Refresh();
    }

    // Back goes one step back and keeps what was typed; on the first step it leaves (asking before it discards)
    [RelayCommand]
    private async Task BackAsync()
    {
        if (Step <= _firstStep)
        {
            await Form.CancelCommand.ExecuteAsync(null);
            return;
        }

        Step--;
        if (Step == 1 && Form.Barcode.Length == 0)
        {
            await Form.StartScanCommand.ExecuteAsync(null);
        }

        Refresh();
    }

    [RelayCommand]
    private async Task PhotoAsync()
    {
        var menu = PhotoMenu.For(Form.Photo);
        await _sheets.ShowAsync(menu);
        var command = menu.Chosen switch
        {
            PhotoMenu.Take => Form.TakePhotoCommand,
            PhotoMenu.Pick => Form.PickPhotoCommand,
            PhotoMenu.Remove => Form.RemovePhotoCommand,
            _ => null
        };
        if (command is not null)
        {
            await command.ExecuteAsync(null);
        }
    }

    // "+ New characteristic" in the same sheet as on the Characteristics page
    [RelayCommand]
    private async Task NewCharacteristicAsync()
    {
        var sheet = new NewCharacteristicViewModel(_characteristics, _validator);
        await _sheets.ShowAsync(sheet);
        await LoadDetailsAsync();
        Refresh();
    }

    // Keeps what was typed; adds the characteristics that are new
    private async Task LoadDetailsAsync()
    {
        foreach (var characteristic in await _characteristics.GetAllAsync())
        {
            if (Details.All(item => item.CharacteristicId != characteristic.Id))
            {
                var item = new CharacteristicEditItem(
                    new CharacteristicValueItem(characteristic.Id, characteristic.Name, characteristic.ValueType, string.Empty),
                    _validator, canEdit: true);
                item.PropertyChanged += OnDetailChanged;
                Details.Add(item);
            }
        }
    }

    private void OnDetailChanged(object? sender, PropertyChangedEventArgs e) => Refresh();

    // What the open database said, where a characteristic of the same meaning is still empty
    private void Prefill()
    {
        if (Form.Found is not { } found)
        {
            return;
        }

        foreach (var item in Details.Where(item => item.Value.Length == 0))
        {
            var value = item.Name.ToUpperInvariant() switch
            {
                "BRAND" => found.Brand,
                "CATEGORY" => found.Category,
                "AUTHOR" or "AUTHORS" => found.Authors,
                "PUBLISHER" => found.Publisher,
                var name when name.StartsWith("WEIGHT", StringComparison.Ordinal) => Kilograms(found.QuantityText),
                var name when name.StartsWith("VOLUME", StringComparison.Ordinal) => Litres(found.QuantityText),
                _ => string.Empty
            };
            if (value.Length > 0 && _validator.ValidateValue(item.ValueType, value) is null)
            {
                item.Value = value;
            }
        }
    }

    // "400 g" -> "0.4", "1.5 kg" -> "1.5"; anything else (millilitres, packs) is no weight
    private static string Kilograms(string quantityText) => Amount(quantityText, WeightPattern(), "g");

    // "330 ml" -> "0.33", "1.5 l" -> "1.5"
    private static string Litres(string quantityText) => Amount(quantityText, VolumePattern(), "ml");

    private static string Amount(string quantityText, Regex pattern, string thousandthUnit)
    {
        var match = pattern.Match(quantityText.Trim());
        if (!match.Success
            || !decimal.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            return string.Empty;
        }

        var whole = match.Groups[2].Value.Equals(thousandthUnit, StringComparison.OrdinalIgnoreCase) ? amount / 1000 : amount;
        return whole.ToString("0.###", CultureInfo.CurrentCulture);
    }

    // The trailing "e" is the EU "estimated quantity" mark
    [GeneratedRegex(@"^(\d+(?:[.,]\d+)?)\s*(g|kg)(?:\s*e)?$", RegexOptions.IgnoreCase)]
    private static partial Regex WeightPattern();

    [GeneratedRegex(@"^(\d+(?:[.,]\d+)?)\s*(ml|l)(?:\s*e)?$", RegexOptions.IgnoreCase)]
    private static partial Regex VolumePattern();

    // Called by the form after the product was added, before it goes back to where the wizard was started
    private async Task<ProductSummary> SaveValuesAsync(ProductSummary product)
    {
        var values = Details.Where(item => item.Value.Trim().Length > 0)
            .ToDictionary(item => item.CharacteristicId, item => (string?)item.Value);
        if (values.Count == 0)
        {
            return product;
        }

        var result = await _characteristics.SetValuesAsync(product.Id, values);
        return result.IsSuccess
            ? product with { Values = values.ToDictionary(pair => pair.Key, pair => pair.Value!.Trim()) }
            : product;
    }

    private void Refresh()
    {
        OnPropertyChanged(nameof(StepText));
        OnPropertyChanged(nameof(Dots));
        OnPropertyChanged(nameof(NextText));
        OnPropertyChanged(nameof(Hint));
        NextCommand.NotifyCanExecuteChanged();
    }
}
