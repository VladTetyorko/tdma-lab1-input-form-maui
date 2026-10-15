using System.Reflection;

namespace Mobile_apps_L1_Model_view.Data.Migrations;

// Reads the numbered scripts compiled into the assembly as "Migrations/0001_initial.sql", ...
public sealed class EmbeddedMigrationSource(Assembly? assembly = null) : IMigrationSource
{
    private const string Prefix = "Migrations/";
    private const string Suffix = ".sql";

    private readonly Assembly _assembly = assembly ?? typeof(EmbeddedMigrationSource).Assembly;

    public IReadOnlyList<Migration> GetMigrations()
    {
        var migrations = _assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(Prefix, StringComparison.Ordinal) && n.EndsWith(Suffix, StringComparison.Ordinal))
            .Select(Read)
            .OrderBy(m => m.Version)
            .ToList();

        var duplicate = migrations.GroupBy(m => m.Version).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Two migrations have version {duplicate.Key}.");
        }

        return migrations;
    }

    private Migration Read(string resourceName)
    {
        var name = resourceName[Prefix.Length..^Suffix.Length];
        var digits = new string(name.TakeWhile(char.IsDigit).ToArray());
        if (!int.TryParse(digits, out var version))
        {
            throw new InvalidOperationException($"Migration '{name}' must start with its version number.");
        }

        using var stream = _assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return new Migration(version, name, reader.ReadToEnd());
    }
}
