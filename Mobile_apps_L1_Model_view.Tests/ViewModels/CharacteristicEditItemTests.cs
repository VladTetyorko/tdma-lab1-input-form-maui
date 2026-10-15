using System.ComponentModel;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class CharacteristicEditItemTests
{
    private readonly ICharacteristicValidator _validator = new TestStore().Validator;

    [Fact]
    public void Constructor_CopiesTheSourceRow()
    {
        var source = new CharacteristicValueItem(Guid.NewGuid(), "Weight (kg)", CharacteristicValueType.Number, "1.5");

        var item = new CharacteristicEditItem(source, _validator, canEdit: true);

        Assert.Equal(source.CharacteristicId, item.CharacteristicId);
        Assert.Equal("Weight (kg)", item.Name);
        Assert.Equal(CharacteristicValueType.Number, item.ValueType);
        Assert.Equal("1.5", item.Value);
        Assert.Equal(string.Empty, item.Error);
    }

    [Theory]
    [InlineData(CharacteristicValueType.Number, "number")]
    [InlineData(CharacteristicValueType.Text, "text")]
    public void Placeholder_DependsOnValueType(CharacteristicValueType type, string expected)
    {
        var item = Item(type, string.Empty, canEdit: true);

        Assert.Equal(expected, item.Placeholder);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void IsReadOnly_IsTheOppositeOfCanEdit(bool canEdit, bool expected)
    {
        var item = Item(CharacteristicValueType.Text, "Blue", canEdit);

        Assert.Equal(expected, item.IsReadOnly);
    }

    [Fact]
    public void Error_ShowsForInvalidNumberAndClearsWhenFixed()
    {
        var item = Item(CharacteristicValueType.Number, string.Empty, canEdit: true);

        item.Value = "abc";
        Assert.Equal("Enter a number, for example 12.5.", item.Error);

        item.Value = "12.5";
        Assert.Equal(string.Empty, item.Error);
    }

    [Fact]
    public void Error_ShowsForTextLongerThanTheLimit()
    {
        var item = Item(CharacteristicValueType.Text, string.Empty, canEdit: true);

        item.Value = new string('x', 101);

        Assert.Equal("Value must be at most 100 characters.", item.Error);
    }

    [Fact]
    public void Error_PropertyChangedIsRaisedWhenTheErrorChanges()
    {
        var item = Item(CharacteristicValueType.Number, string.Empty, canEdit: true);
        var raised = new List<string?>();
        item.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        item.Value = "abc";

        Assert.Contains(nameof(CharacteristicEditItem.Error), raised);
    }

    private CharacteristicEditItem Item(CharacteristicValueType type, string value, bool canEdit) =>
        new(new CharacteristicValueItem(Guid.NewGuid(), "Field", type, value), _validator, canEdit);
}
