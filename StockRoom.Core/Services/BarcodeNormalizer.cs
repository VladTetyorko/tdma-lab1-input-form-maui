using System.Text.RegularExpressions;

namespace Mobile_apps_L1_Model_view.Services;

// StockRoom stores and compares one form of a code, the 13 digits of an EAN-13 (GTIN-13). The other retail forms
// are turned into it here: UPC-A and EAN-8 get leading zeros (the check digit stays valid), UPC-E is expanded to
// UPC-A first, an ISBN-10 becomes its 978 ISBN-13
public static partial class BarcodeNormalizer
{
    private const int UpcALength = Ean13.Length - 1;
    private const int ShortLength = 8;
    private const int Isbn10Length = 10;

    // Null when the text is none of these codes or its check digit is wrong. Spaces and hyphens are ignored
    public static string? ToEan13(string? text)
    {
        var trimmed = text?.Trim();
        if (trimmed is null || !CodePattern().IsMatch(trimmed))
        {
            return null;
        }

        var code = trimmed.Replace(" ", string.Empty).Replace("-", string.Empty).ToUpperInvariant();
        return code.Length switch
        {
            Isbn10Length => FromIsbn10(code),
            ShortLength => FromUpcE(code) ?? Padded(code),
            UpcALength or Ean13.Length => Padded(code),
            _ => null
        };
    }

    private static string? Padded(string digits)
    {
        var padded = digits.PadLeft(Ean13.Length, '0');
        return Ean13.IsValid(padded) ? padded : null;
    }

    // UPC-E (number system 0) is a UPC-A with zeros left out; the last of its six digits says where they were.
    // The check digit is that of the full UPC-A. EAN-8 codes starting with 0 are reserved for in-store use,
    // so UPC-E is tried first
    private static string? FromUpcE(string code)
    {
        if (code[0] != '0' || !code.All(char.IsAsciiDigit))
        {
            return null;
        }

        var d = code.Substring(1, 6);
        var body = d[5] switch
        {
            '0' or '1' or '2' => $"{d[..2]}{d[5]}0000{d[2..5]}",
            '3' => $"{d[..3]}00000{d[3..5]}",
            '4' => $"{d[..4]}00000{d[4]}",
            _ => $"{d[..5]}0000{d[5]}"
        };
        return Padded($"0{body}{code[^1]}");
    }

    // Weights 10..1, the sum divisible by 11; X stands for 10 and only as the last character
    private static string? FromIsbn10(string code)
    {
        var sum = 0;
        for (var i = 0; i < Isbn10Length; i++)
        {
            var value = char.IsAsciiDigit(code[i]) ? code[i] - '0'
                : code[i] == 'X' && i == Isbn10Length - 1 ? 10
                : -1;
            if (value < 0)
            {
                return null;
            }

            sum += value * (Isbn10Length - i);
        }

        return sum % 11 == 0 ? Ean13.WithCheckDigit("978" + code[..^1]) : null;
    }

    [GeneratedRegex("^[0-9][0-9 -]*[0-9Xx]$")]
    private static partial Regex CodePattern();
}
