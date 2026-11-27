using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Mobile_apps_L1_Model_view.Tests.Resources;

// A mistyped resource key fails silently (DynamicResource) or only when the page opens (StaticResource): check them all here
public class ResourceKeyTests
{
    private static readonly Regex Reference = new(@"\{(?:Static|Dynamic)Resource\s+(?<key>\w+)\}");

    [Fact]
    public void EveryResourceReferenceInTheXaml_HasADefinition()
    {
        var shared = new[]
        {
            XamlFiles.Load("Resources", "Styles", "Styles.xaml"),
            XamlFiles.Load("Resources", "Styles", "Icons.xaml"),
            XamlFiles.Load("Resources", "Styles", "Themes", "LightGreen.xaml")
        }.SelectMany(document => XamlFiles.Keys(document).Keys).ToHashSet();

        var excluded = new[] { "obj", "bin", "Platforms" }
            .Select(folder => $"{Path.DirectorySeparatorChar}{folder}{Path.DirectorySeparatorChar}")
            .ToList();
        var missing = Directory.GetFiles(XamlFiles.App, "*.xaml", SearchOption.AllDirectories)
            .Where(file => !excluded.Any(file.Contains))
            .SelectMany(file =>
            {
                var local = XamlFiles.Keys(XDocument.Load(file)).Keys;
                return Reference.Matches(File.ReadAllText(file))
                    .Select(match => match.Groups["key"].Value)
                    .Where(key => !shared.Contains(key) && !local.Contains(key))
                    .Select(key => $"{Path.GetFileName(file)}: {key}");
            })
            .Distinct()
            .ToList();

        Assert.Empty(missing);
    }
}
