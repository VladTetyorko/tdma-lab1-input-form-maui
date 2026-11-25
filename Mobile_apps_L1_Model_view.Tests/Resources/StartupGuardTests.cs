using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Resources;

// What would crash the app at start or when a page opens, but compiles: a service nobody registered, a resource key that
// means two things, a XAML type taken from the wrong assembly. The app project is read as text, so no device is needed
public class StartupGuardTests
{
    private static readonly string Program = File.ReadAllText(Path.Combine(XamlFiles.App, "MauiProgram.cs"));

    private static readonly string Core = Path.Combine(Path.GetDirectoryName(XamlFiles.App)!, "StockRoom.Core");

    [Fact]
    public void CoreServiceGraph_Builds()
    {
        var services = new ServiceCollection().AddStockRoomCore().AddFakePlatform();
        services.AddSingleton<IPreferencesStore, FakePreferencesStore>();
        services.AddSingleton<INavigationService, FakeNavigationService>();
        services.AddSingleton<IAlertService, FakeAlertService>();
        services.AddSingleton<IThemeApplier, FakeThemeApplier>();
        services.AddSingleton<IScanFeedback, FakeScanFeedback>();
        services.AddSingleton<IToastService, FakeToastService>();
        services.AddSingleton<IBarcodeScanner, FakeBarcodeScanner>();
        services.AddSingleton<INavigationDrawer, FakeNavigationDrawer>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }

    // Every service a Core class asks for and Core does not register must come from MauiProgram
    [Fact]
    public void EveryPlatformServiceCoreNeeds_IsRegisteredByTheApp()
    {
        var descriptors = new ServiceCollection().AddStockRoomCore().ToList();
        var inCore = descriptors.Select(descriptor => descriptor.ServiceType).ToHashSet();

        var missing = descriptors
            .Select(descriptor => descriptor.ImplementationType)
            .OfType<Type>()
            .Where(type => type.Assembly == typeof(Routes).Assembly)
            .SelectMany(type => type.GetConstructors().SelectMany(constructor => constructor.GetParameters()))
            .Where(parameter => !parameter.HasDefaultValue)
            .Select(parameter => parameter.ParameterType)
            .Where(type => type.Assembly == typeof(Routes).Assembly && !inCore.Contains(type))
            .Distinct()
            .Where(type => !IsRegisteredByTheApp(type.Name))
            .Select(type => type.Name)
            .ToList();

        Assert.Empty(missing);
    }

    // A page the app asks the container (or a Shell route) for must be registered, and so must what its constructor takes
    [Fact]
    public void EveryPageAndItsConstructorArguments_AreRegistered()
    {
        var core = new ServiceCollection().AddStockRoomCore().Select(descriptor => descriptor.ServiceType.Name).ToHashSet();
        var constructor = new Regex(@"public (?<page>\w+Page)\((?<parameters>[^)]*)\)");

        var pages = Directory.GetFiles(Path.Combine(XamlFiles.App, "Views"), "*Page.cs", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(Path.Combine(XamlFiles.App, "Views"), "*Page.xaml.cs", SearchOption.TopDirectoryOnly))
            .Select(File.ReadAllText)
            .SelectMany(text => constructor.Matches(text))
            .ToList();
        var missing = new List<string>();
        foreach (var match in pages)
        {
            var page = match.Groups["page"].Value;
            var parameters = match.Groups["parameters"].Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(parameter => parameter.Split(' ')[0])
                .ToList();

            // StartupPage is created before the container is used, with a status object
            if (page == "StartupPage")
            {
                continue;
            }

            // A page with a value argument is built by hand; only its services are checked
            if (parameters.All(type => type.EndsWith("ViewModel", StringComparison.Ordinal)) && !IsRegisteredByTheApp(page))
            {
                missing.Add(page);
            }

            missing.AddRange(parameters
                .Where(type => type.EndsWith("ViewModel", StringComparison.Ordinal) && !core.Contains(type))
                .Select(type => $"{page}: {type}"));
        }

        Assert.NotEmpty(pages);
        Assert.Empty(missing);
    }

    [Fact]
    public void NoResourceKey_IsDefinedInTwoDictionaries()
    {
        var shared = XamlFiles.Keys(XamlFiles.Load("Resources", "Styles", "Styles.xaml")).Keys
            .Concat(XamlFiles.Keys(XamlFiles.Load("Resources", "Styles", "Icons.xaml")).Keys)
            .ToList();
        var theme = XamlFiles.Keys(XamlFiles.Load("Resources", "Styles", "Themes", "LightGreen.xaml")).Keys;

        Assert.Empty(shared.Intersect(theme));
        Assert.Empty(shared.GroupBy(key => key).Where(group => group.Count() > 1).Select(group => group.Key));
    }

    // Keys named in code (icons of the menu, colours set on elements built in code) are not seen by the XAML check
    [Fact]
    public void EveryResourceKeyNamedInCode_Exists()
    {
        var keys = new[]
        {
            XamlFiles.Load("Resources", "Styles", "Styles.xaml"),
            XamlFiles.Load("Resources", "Styles", "Icons.xaml"),
            XamlFiles.Load("Resources", "Styles", "Themes", "LightGreen.xaml")
        }.SelectMany(document => XamlFiles.Keys(document).Keys).ToHashSet();
        var reference = new Regex(@"(?:SetDynamicResource\([^,]+,\s*|Resources\[)""(?<key>\w+)""|""(?<key>Icon[A-Z]\w+)""");

        var missing = SourceFiles(XamlFiles.App, "*.cs")
            .SelectMany(file => reference.Matches(File.ReadAllText(file)))
            .Select(match => match.Groups["key"].Value)
            .Where(key => !keys.Contains(key))
            .Distinct()
            .ToList();

        Assert.Empty(missing);
    }

    // "clr-namespace:X" means the app assembly; a type that lives in Core needs ";assembly=StockRoom.Core" (and only then)
    [Fact]
    public void EveryXamlType_IsDeclaredInTheAssemblyItsNamespaceNames()
    {
        var appTypes = DeclaredTypes(XamlFiles.App);
        var coreTypes = DeclaredTypes(Core);
        var missing = new List<string>();

        foreach (var file in SourceFiles(XamlFiles.App, "*.xaml"))
        {
            var text = File.ReadAllText(file);
            var namespaces = XDocument.Parse(text).Root!.Attributes()
                .Where(attribute => attribute.IsNamespaceDeclaration && attribute.Value.StartsWith("clr-namespace:", StringComparison.Ordinal));
            foreach (var declaration in namespaces)
            {
                var parts = declaration.Value["clr-namespace:".Length..].Split(';');
                var types = declaration.Value.EndsWith(";assembly=StockRoom.Core", StringComparison.Ordinal) ? coreTypes : appTypes;
                var prefix = declaration.Name.LocalName;
                // <p:Type ...>, {x:Type p:Type}, {x:Static p:Type.Member}, x:DataType="p:Type"; attached members excluded
                var used = Regex.Matches(text, $@"(?<![\w:]){prefix}:(?<type>\w+)").Select(match => match.Groups["type"].Value);
                missing.AddRange(used
                    .Where(type => !types.Contains($"{parts[0]}.{type}"))
                    .Select(type => $"{Path.GetFileName(file)}: {prefix}:{type}"));
            }
        }

        Assert.Empty(missing.Distinct());
    }

    private static bool IsRegisteredByTheApp(string type) =>
        Regex.IsMatch(Program, $@"Add(Singleton|Transient)<{type}[,>]");

    private static IEnumerable<string> SourceFiles(string root, string pattern) =>
        Directory.GetFiles(root, pattern, SearchOption.AllDirectories)
            .Where(file => !Path.GetRelativePath(root, file).Split(Path.DirectorySeparatorChar)
                .Any(part => part is "obj" or "bin" or "Platforms"));

    private static HashSet<string> DeclaredTypes(string root)
    {
        var types = new HashSet<string>();
        foreach (var text in SourceFiles(root, "*.cs").Select(File.ReadAllText))
        {
            var space = Regex.Match(text, @"^namespace\s+(?<name>[\w.]+)", RegexOptions.Multiline).Groups["name"].Value;
            foreach (Match match in Regex.Matches(text, @"\b(?:class|record|enum|interface|struct)\s+(?<type>[A-Z]\w*)"))
            {
                types.Add($"{space}.{match.Groups["type"].Value}");
            }
        }

        return types;
    }
}
