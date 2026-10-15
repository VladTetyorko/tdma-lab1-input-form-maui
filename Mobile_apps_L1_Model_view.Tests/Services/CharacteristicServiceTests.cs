using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class CharacteristicServiceTests
{
    private const string ValidBarcode = "4006381333931";

    private readonly TestStore _store = new();
    private readonly ICharacteristicService _service;

    public CharacteristicServiceTests()
    {
        _service = _store.CharacteristicService;
    }

    [Fact]
    public async Task AddAsync_NotSignedIn_Fails()
    {
        var result = await _service.AddAsync("Color", CharacteristicValueType.Text);

        Assert.False(result.IsSuccess);
        Assert.Equal("Sign in before you add characteristics.", result.Error);
        Assert.Empty(await _store.Characteristics.GetAllAsync());
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task AddAsync_NonManager_Fails(Role role)
    {
        await _store.SignInAsync(role);

        var result = await _service.AddAsync("Color", CharacteristicValueType.Text);

        Assert.False(result.IsSuccess);
        Assert.Equal($"Your role ({role}) cannot add characteristics.", result.Error);
        Assert.Empty(await _store.Characteristics.GetAllAsync());
    }

    [Fact]
    public async Task AddAsync_Manager_StoresTrimmedCharacteristicOwnedByManager()
    {
        var manager = await _store.SignInAsync(Role.Manager);

        var result = await _service.AddAsync("  Weight (kg)  ", CharacteristicValueType.Number);

        Assert.True(result.IsSuccess);
        var stored = Assert.Single(await _store.Characteristics.GetAllAsync());
        Assert.Equal("Weight (kg)", stored.Name);
        Assert.Equal(CharacteristicValueType.Number, stored.ValueType);
        Assert.Equal(manager.Id, stored.CreatedByUserId);
        Assert.Equal(_store.Clock.UtcNow, stored.CreatedAt);
        Assert.Same(stored, result.Value);
    }

    [Fact]
    public async Task AddAsync_DuplicateNameIgnoringCase_FailsAndStoresNothing()
    {
        await _store.SignInAsync(Role.Manager);
        await _service.AddAsync("Color", CharacteristicValueType.Text);

        var result = await _service.AddAsync("COLOR", CharacteristicValueType.Text);

        Assert.Equal("A characteristic with this name already exists.", result.Error);
        Assert.Single(await _store.Characteristics.GetAllAsync());
    }

    [Fact]
    public async Task AddAsync_BlankName_Fails()
    {
        await _store.SignInAsync(Role.Manager);

        var result = await _service.AddAsync("   ", CharacteristicValueType.Text);

        Assert.Equal("Name is required.", result.Error);
        Assert.Empty(await _store.Characteristics.GetAllAsync());
    }

    [Fact]
    public async Task SetValuesAsync_NotSignedIn_Fails()
    {
        var product = await _store.AddProductAsync("Drill", ValidBarcode);

        var result = await _service.SetValuesAsync(product.Id, new Dictionary<Guid, string?>());

        Assert.Equal("Sign in before you edit characteristic values.", result.Error);
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task SetValuesAsync_NonManager_FailsAndChangesNothing(Role role)
    {
        var product = await _store.AddProductAsync("Drill", ValidBarcode);
        var color = await _store.AddCharacteristicAsync("Color", CharacteristicValueType.Text);
        await _store.SetValueAsync(product, color, "Blue");
        await _store.SignInAsync(role);

        var result = await _service.SetValuesAsync(product.Id, new Dictionary<Guid, string?> { [color.Id] = "Red" });

        Assert.Equal($"Your role ({role}) cannot edit characteristic values.", result.Error);
        Assert.Equal("Blue", Assert.Single(await _store.Values.GetAllAsync()).Value);
    }

    [Fact]
    public async Task SetValuesAsync_UnknownProduct_Fails()
    {
        await _store.SignInAsync(Role.Manager);

        var result = await _service.SetValuesAsync(Guid.NewGuid(), new Dictionary<Guid, string?>());

        Assert.Equal("Product not found.", result.Error);
    }

    [Fact]
    public async Task SetValuesAsync_UnknownCharacteristic_Fails()
    {
        await _store.SignInAsync(Role.Manager);
        var product = await _store.AddProductAsync("Drill", ValidBarcode);

        var result = await _service.SetValuesAsync(
            product.Id, new Dictionary<Guid, string?> { [Guid.NewGuid()] = "Red" });

        Assert.Equal("Characteristic not found.", result.Error);
        Assert.Empty(await _store.Values.GetAllAsync());
    }

    [Fact]
    public async Task SetValuesAsync_InvalidNumber_ChangesNothing()
    {
        await _store.SignInAsync(Role.Manager);
        var product = await _store.AddProductAsync("Drill", ValidBarcode);
        var weight = await _store.AddCharacteristicAsync("Weight (kg)", CharacteristicValueType.Number);
        var color = await _store.AddCharacteristicAsync("Color", CharacteristicValueType.Text);
        await _store.SetValueAsync(product, weight, "1.5");
        await _store.SetValueAsync(product, color, "Blue");

        var result = await _service.SetValuesAsync(product.Id, new Dictionary<Guid, string?>
        {
            [color.Id] = "Red",
            [weight.Id] = "abc"
        });

        Assert.Equal("Weight (kg): Enter a number, for example 12.5.", result.Error);
        var stored = (await _store.Values.GetByProductAsync(product.Id)).ToDictionary(v => v.CharacteristicId);
        Assert.Equal("1.5", stored[weight.Id].Value);
        Assert.Equal("Blue", stored[color.Id].Value);
        Assert.Equal(2, (await _store.Values.GetAllAsync()).Count);
    }

    [Fact]
    public async Task SetValuesAsync_NewValue_CreatesStoredValueWithTrimmedText()
    {
        var manager = await _store.SignInAsync(Role.Manager);
        var product = await _store.AddProductAsync("Drill", ValidBarcode);
        var color = await _store.AddCharacteristicAsync("Color", CharacteristicValueType.Text);

        var result = await _service.SetValuesAsync(product.Id, new Dictionary<Guid, string?> { [color.Id] = "  Red " });

        Assert.Equal(1, result.Value);
        var stored = Assert.Single(await _store.Values.GetByProductAsync(product.Id));
        Assert.Equal(color.Id, stored.CharacteristicId);
        Assert.Equal("Red", stored.Value);
        Assert.Equal(manager.Id, stored.CreatedByUserId);
        Assert.Equal(_store.Clock.UtcNow, stored.CreatedAt);
    }

    [Fact]
    public async Task SetValuesAsync_NumberValue_IsStoredInCanonicalForm()
    {
        await _store.SignInAsync(Role.Manager);
        var product = await _store.AddProductAsync("Drill", ValidBarcode);
        var weight = await _store.AddCharacteristicAsync("Weight (kg)", CharacteristicValueType.Number);

        var result = await _service.SetValuesAsync(product.Id, new Dictionary<Guid, string?> { [weight.Id] = "01.50" });

        Assert.Equal(1, result.Value);
        Assert.Equal("1.5", Assert.Single(await _store.Values.GetAllAsync()).Value);
    }

    [Fact]
    public async Task SetValuesAsync_ChangedValue_UpdatesStoredRowAndUpdatedAt()
    {
        await _store.SignInAsync(Role.Manager);
        var product = await _store.AddProductAsync("Drill", ValidBarcode);
        var weight = await _store.AddCharacteristicAsync("Weight (kg)", CharacteristicValueType.Number);
        await _store.SetValueAsync(product, weight, "1.5");
        var before = Assert.Single(await _store.Values.GetAllAsync());
        _store.Clock.UtcNow = _store.Clock.UtcNow.AddHours(2);

        var result = await _service.SetValuesAsync(product.Id, new Dictionary<Guid, string?> { [weight.Id] = "2" });

        Assert.Equal(1, result.Value);
        var after = Assert.Single(await _store.Values.GetAllAsync());
        Assert.Equal(before.Id, after.Id);
        Assert.Equal("2", after.Value);
        Assert.Equal(_store.Clock.UtcNow, after.UpdatedAt);
    }

    [Fact]
    public async Task SetValuesAsync_SameValueInOtherForm_CountsAsUnchanged()
    {
        await _store.SignInAsync(Role.Manager);
        var product = await _store.AddProductAsync("Drill", ValidBarcode);
        var weight = await _store.AddCharacteristicAsync("Weight (kg)", CharacteristicValueType.Number);
        await _store.SetValueAsync(product, weight, "1.5");
        var updatedBefore = Assert.Single(await _store.Values.GetAllAsync()).UpdatedAt;
        _store.Clock.UtcNow = _store.Clock.UtcNow.AddHours(2);

        var result = await _service.SetValuesAsync(product.Id, new Dictionary<Guid, string?> { [weight.Id] = "01.50" });

        Assert.Equal(0, result.Value);
        var stored = Assert.Single(await _store.Values.GetAllAsync());
        Assert.Equal("1.5", stored.Value);
        Assert.Equal(updatedBefore, stored.UpdatedAt);
    }

    [Fact]
    public async Task SetValuesAsync_EmptyValue_RemovesStoredValue()
    {
        await _store.SignInAsync(Role.Manager);
        var product = await _store.AddProductAsync("Drill", ValidBarcode);
        var color = await _store.AddCharacteristicAsync("Color", CharacteristicValueType.Text);
        await _store.SetValueAsync(product, color, "Blue");

        var result = await _service.SetValuesAsync(product.Id, new Dictionary<Guid, string?> { [color.Id] = "   " });

        Assert.Equal(1, result.Value);
        Assert.Empty(await _store.Values.GetByProductAsync(product.Id));
    }

    [Fact]
    public async Task SetValuesAsync_EmptyValueWithNothingStored_CountsZero()
    {
        await _store.SignInAsync(Role.Manager);
        var product = await _store.AddProductAsync("Drill", ValidBarcode);
        var color = await _store.AddCharacteristicAsync("Color", CharacteristicValueType.Text);

        var result = await _service.SetValuesAsync(product.Id, new Dictionary<Guid, string?> { [color.Id] = null });

        Assert.Equal(0, result.Value);
        Assert.Empty(await _store.Values.GetAllAsync());
    }

    [Fact]
    public async Task SetValuesAsync_MixedChanges_ReturnsNumberOfChangedValues()
    {
        await _store.SignInAsync(Role.Manager);
        var product = await _store.AddProductAsync("Drill", ValidBarcode);
        var weight = await _store.AddCharacteristicAsync("Weight (kg)", CharacteristicValueType.Number);
        var volume = await _store.AddCharacteristicAsync("Volume (l)", CharacteristicValueType.Number);
        var color = await _store.AddCharacteristicAsync("Color", CharacteristicValueType.Text);
        var category = await _store.AddCharacteristicAsync("Category", CharacteristicValueType.Text);
        await _store.SetValueAsync(product, weight, "1.5");   // unchanged
        await _store.SetValueAsync(product, volume, "3");     // changed
        await _store.SetValueAsync(product, color, "Blue");   // removed
        // category: created

        var result = await _service.SetValuesAsync(product.Id, new Dictionary<Guid, string?>
        {
            [weight.Id] = "1.5",
            [volume.Id] = "4",
            [color.Id] = "",
            [category.Id] = "Tools"
        });

        Assert.Equal(3, result.Value);
    }
}
