using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class CharacteristicService(
    ICharacteristicRepository characteristics,
    IProductCharacteristicRepository values,
    IProductRepository products,
    ICharacteristicValidator validator,
    ICurrentUserService currentUser,
    IClock clock) : ICharacteristicService
{
    public Task<IReadOnlyList<Characteristic>> GetAllAsync() => characteristics.GetAllAsync();

    public async Task<Result<Characteristic>> AddAsync(string? name, CharacteristicValueType valueType)
    {
        var denied = CheckManager("add characteristics");
        if (denied is not null)
        {
            return Result<Characteristic>.Failure(denied);
        }

        var error = await validator.ValidateNameAsync(name);
        if (error is not null)
        {
            return Result<Characteristic>.Failure(error);
        }

        var now = clock.UtcNow;
        var characteristic = new Characteristic
        {
            Name = name!.Trim(),
            ValueType = valueType,
            CreatedByUserId = currentUser.CurrentUser!.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
        await characteristics.AddAsync(characteristic);
        return Result<Characteristic>.Success(characteristic);
    }

    public async Task<Result<int>> SetValuesAsync(Guid productId, IReadOnlyDictionary<Guid, string?> newValues)
    {
        var denied = CheckManager("edit characteristic values");
        if (denied is not null)
        {
            return Result<int>.Failure(denied);
        }

        if (await products.GetByIdAsync(productId) is null)
        {
            return Result<int>.Failure("Product not found.");
        }

        var known = (await characteristics.GetAllAsync()).ToDictionary(c => c.Id);
        foreach (var (characteristicId, value) in newValues)
        {
            if (!known.TryGetValue(characteristicId, out var characteristic))
            {
                return Result<int>.Failure("Characteristic not found.");
            }

            var error = validator.ValidateValue(characteristic.ValueType, value);
            if (error is not null)
            {
                return Result<int>.Failure($"{characteristic.Name}: {error}");
            }
        }

        var stored = (await values.GetByProductAsync(productId)).ToDictionary(v => v.CharacteristicId);
        var changes = 0;
        foreach (var (characteristicId, value) in newValues)
        {
            var text = CharacteristicValue.Normalize(known[characteristicId].ValueType, value);
            stored.TryGetValue(characteristicId, out var current);
            changes += await ApplyAsync(productId, characteristicId, current, text);
        }

        return Result<int>.Success(changes);
    }

    // Returns 1 when something was written, 0 when the value was already as requested
    private async Task<int> ApplyAsync(Guid productId, Guid characteristicId, ProductCharacteristic? current, string text)
    {
        if (current is null)
        {
            if (text.Length == 0)
            {
                return 0;
            }

            var now = clock.UtcNow;
            await values.AddAsync(new ProductCharacteristic
            {
                ProductId = productId,
                CharacteristicId = characteristicId,
                Value = text,
                CreatedByUserId = currentUser.CurrentUser!.Id,
                CreatedAt = now,
                UpdatedAt = now
            });
            return 1;
        }

        if (text.Length == 0)
        {
            await values.DeleteAsync(current.Id);
            return 1;
        }

        if (current.Value == text)
        {
            return 0;
        }

        current.Value = text;
        current.UpdatedAt = clock.UtcNow;
        await values.UpdateAsync(current);
        return 1;
    }

    // Null when the current user may manage; otherwise the message for the user
    private string? CheckManager(string action)
    {
        var user = currentUser.CurrentUser;
        if (user is null)
        {
            return $"Sign in before you {action}.";
        }

        return RolePermissions.CanManageProducts(user.Role) ? null : $"Your role ({user.Role}) cannot {action}.";
    }
}
