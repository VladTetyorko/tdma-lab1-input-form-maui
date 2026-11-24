namespace Mobile_apps_L1_Model_view.Tests.Fakes;

public static class TokenTamper
{
    // Changes one character in the middle of the signature (the part after the dot). Not the last character:
    // its low bits are padding in base64, so changing only those would still decode to the same bytes
    public static string ChangeSignature(string token)
    {
        var index = token.IndexOf('.') + 1 + 10;
        var replacement = token[index] == 'A' ? 'B' : 'A';
        return token[..index] + replacement + token[(index + 1)..];
    }
}
