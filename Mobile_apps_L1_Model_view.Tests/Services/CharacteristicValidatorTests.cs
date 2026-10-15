using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class CharacteristicValidatorTests
{
    private readonly InMemoryCharacteristicRepository _characteristics = new();
    private readonly CharacteristicValidator _validator;

    public CharacteristicValidatorTests()
    {
        _validator = new CharacteristicValidator(_characteristics);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateName_BlankName_RequiresName(string? name)
    {
        Assert.Equal("Name is required.", _validator.ValidateName(name));
    }

    [Fact]
    public void ValidateName_FiftyCharacters_IsValid()
    {
        Assert.Null(_validator.ValidateName(new string('a', 50)));
    }

    [Fact]
    public void ValidateName_FiftyOneCharacters_IsTooLong()
    {
        Assert.Equal("Name must be at most 50 characters.", _validator.ValidateName(new string('a', 51)));
    }

    [Fact]
    public void ValidateName_LengthIsCountedAfterTrimming()
    {
        Assert.Null(_validator.ValidateName("   " + new string('a', 50) + "   "));
    }

    [Fact]
    public async Task ValidateNameAsync_NewName_IsValid()
    {
        Assert.Null(await _validator.ValidateNameAsync("Weight (kg)"));
    }

    [Theory]
    [InlineData("Color")]
    [InlineData("color")]
    [InlineData("  COLOR  ")]
    public async Task ValidateNameAsync_ExistingNameIgnoringCase_IsDuplicate(string name)
    {
        await _characteristics.AddAsync(new Characteristic { Name = "Color", ValueType = CharacteristicValueType.Text });

        Assert.Equal("A characteristic with this name already exists.", await _validator.ValidateNameAsync(name));
    }

    [Fact]
    public async Task ValidateNameAsync_BlankName_ReportsRequiredBeforeLookingUp()
    {
        Assert.Equal("Name is required.", await _validator.ValidateNameAsync("  "));
    }

    [Fact]
    public void ValidateValue_EmptyTextOrNull_IsValidMeaningNoValue()
    {
        Assert.Null(_validator.ValidateValue(CharacteristicValueType.Text, null));
        Assert.Null(_validator.ValidateValue(CharacteristicValueType.Text, ""));
        Assert.Null(_validator.ValidateValue(CharacteristicValueType.Number, "   "));
    }

    [Fact]
    public void ValidateValue_Text_HundredCharactersIsValid()
    {
        Assert.Null(_validator.ValidateValue(CharacteristicValueType.Text, new string('x', 100)));
    }

    [Fact]
    public void ValidateValue_Text_HundredOneCharactersIsTooLong()
    {
        Assert.Equal(
            "Value must be at most 100 characters.",
            _validator.ValidateValue(CharacteristicValueType.Text, new string('x', 101)));
    }

    [Theory]
    [InlineData("12.5")]
    [InlineData("0")]
    [InlineData("-4")]
    public void ValidateValue_Number_AcceptsNumbers(string value)
    {
        Assert.Null(_validator.ValidateValue(CharacteristicValueType.Number, value));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("1,5")]
    public void ValidateValue_Number_RejectsGarbageNaNAndInfinity(string value)
    {
        Assert.Equal(
            "Enter a number, for example 12.5.",
            _validator.ValidateValue(CharacteristicValueType.Number, value));
    }
}
