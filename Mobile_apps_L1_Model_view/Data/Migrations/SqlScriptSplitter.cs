using System.Text;

namespace Mobile_apps_L1_Model_view.Data.Migrations;

// Splits a script into single statements (sqlite-net runs one statement per call).
// Understands quoted string literals and -- comments; it does not support triggers (BEGIN ... END).
public static class SqlScriptSplitter
{
    private const char Quote = '\'';

    public static IReadOnlyList<string> Split(string script)
    {
        var statements = new List<string>();
        var current = new StringBuilder();
        var inString = false;

        for (var i = 0; i < script.Length; i++)
        {
            var c = script[i];
            if (inString)
            {
                // An escaped quote ('') closes and reopens the literal, which this handles naturally
                current.Append(c);
                inString = c != Quote;
            }
            else if (c == Quote)
            {
                current.Append(c);
                inString = true;
            }
            else if (c == '-' && i + 1 < script.Length && script[i + 1] == '-')
            {
                while (i < script.Length && script[i] != '\n')
                {
                    i++;
                }

                current.Append('\n');
            }
            else if (c == ';')
            {
                Flush(statements, current);
            }
            else
            {
                current.Append(c);
            }
        }

        Flush(statements, current);
        return statements;
    }

    private static void Flush(List<string> statements, StringBuilder current)
    {
        var text = current.ToString().Trim();
        if (text.Length > 0)
        {
            statements.Add(text);
        }

        current.Clear();
    }
}
