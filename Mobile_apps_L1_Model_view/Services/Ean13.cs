namespace Mobile_apps_L1_Model_view.Services;

// EAN-13 rules in one place: 12 data digits plus a check digit
public static class Ean13
{
    public const int Length = 13;

    public static bool IsValid(string? code) =>
        code is { Length: Length } && IsDigits(code) && code[^1] - '0' == ComputeCheckDigit(code[..^1]);

    public static int ComputeCheckDigit(string first12Digits)
    {
        if (first12Digits is not { Length: Length - 1 } || !IsDigits(first12Digits))
        {
            throw new ArgumentException("Exactly 12 digits are required.", nameof(first12Digits));
        }

        var sum = 0;
        for (var i = 0; i < first12Digits.Length; i++)
        {
            var weight = i % 2 == 0 ? 1 : 3;
            sum += (first12Digits[i] - '0') * weight;
        }

        return (10 - sum % 10) % 10;
    }

    public static string WithCheckDigit(string first12Digits) =>
        first12Digits + ComputeCheckDigit(first12Digits);

    private static bool IsDigits(string text) => text.All(char.IsAsciiDigit);
}
