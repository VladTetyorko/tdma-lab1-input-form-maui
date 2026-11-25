using System.Xml.Linq;

namespace Mobile_apps_L1_Model_view.Tests.Resources;

// Reads the app's XAML as XML from the repository, so resources can be checked without MAUI or a UI thread
internal static class XamlFiles
{
    private static readonly XNamespace X = "http://schemas.microsoft.com/winfx/2009/xaml";

    public static string App { get; } = Path.Combine(FindRoot(), "Mobile_apps_L1_Model_view");

    public static XDocument Load(params string[] parts) => XDocument.Load(Path.Combine([App, .. parts]));

    // x:Key -> element name ("Color", "SolidColorBrush", "Style", ...)
    public static Dictionary<string, string> Keys(XDocument document) => document.Descendants()
        .Where(element => element.Attribute(X + "Key") is not null)
        .ToDictionary(element => element.Attribute(X + "Key")!.Value, element => element.Name.LocalName);

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Mobile_apps_L1_Model_view.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found");
    }
}
