using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

// Certificates are generated in the test (self-signed): no file, no network
public class CertificatePinningTests : IDisposable
{
    private readonly List<X509Certificate2> _certificates = [];

    public void Dispose() => _certificates.ForEach(c => c.Dispose());

    private X509Certificate2 NewCertificate(string name)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={name}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        _certificates.Add(certificate);
        return certificate;
    }

    [Fact]
    public void Pin_IsTheBase64Sha256OfTheSubjectPublicKeyInfo()
    {
        var certificate = NewCertificate("server");

        var expected = Convert.ToBase64String(SHA256.HashData(certificate.PublicKey.ExportSubjectPublicKeyInfo()));

        Assert.Equal(expected, CertificatePinValidator.ComputePin(certificate));
        Assert.Equal(32, Convert.FromBase64String(expected).Length);
    }

    [Fact]
    public void Validator_CertificateWithAConfiguredPin_IsPinned()
    {
        var certificate = NewCertificate("server");
        var validator = new CertificatePinValidator(["unrelated-pin", CertificatePinValidator.ComputePin(certificate)]);

        Assert.True(validator.HasPins);
        Assert.True(validator.IsPinned(certificate));
    }

    [Fact]
    public void Validator_AcceptsThePinWithTheSha256Prefix_AndSurroundingSpaces()
    {
        var certificate = NewCertificate("server");

        var validator = new CertificatePinValidator([$"  sha256/{CertificatePinValidator.ComputePin(certificate)} "]);

        Assert.True(validator.IsPinned(certificate));
    }

    [Fact]
    public void Validator_AnotherCertificate_IsNotPinned()
    {
        var validator = new CertificatePinValidator([CertificatePinValidator.ComputePin(NewCertificate("server"))]);

        Assert.False(validator.IsPinned(NewCertificate("attacker")));
    }

    [Fact]
    public void Validator_SameKeyInARenewedCertificate_StaysPinned()
    {
        using var key = RSA.Create(2048);
        var first = new CertificateRequest("CN=server", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow.AddDays(-1));
        var renewed = new CertificateRequest("CN=server", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(90));
        _certificates.AddRange([first, renewed]);

        var validator = new CertificatePinValidator([CertificatePinValidator.ComputePin(first)]);

        Assert.True(validator.IsPinned(renewed));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validator_WithoutPins_HasNoPins_AndPinsNothing(string blankPin)
    {
        var validator = new CertificatePinValidator([blankPin]);

        Assert.False(validator.HasPins);
        Assert.False(validator.IsPinned(NewCertificate("server")));
    }

    // ----- the handler callback -----

    [Fact]
    public void Handler_MatchingPinAndAValidChain_IsAccepted()
    {
        var certificate = NewCertificate("server");
        var validator = new CertificatePinValidator([CertificatePinValidator.ComputePin(certificate)]);

        Assert.True(PinnedHandlerFactory.Accepts(validator, certificate, SslPolicyErrors.None));
    }

    [Fact]
    public void Handler_AnotherCertificate_IsRefused_EvenWithAValidChain()
    {
        var validator = new CertificatePinValidator([CertificatePinValidator.ComputePin(NewCertificate("server"))]);

        Assert.False(PinnedHandlerFactory.Accepts(validator, NewCertificate("attacker"), SslPolicyErrors.None));
    }

    [Fact]
    public void Handler_NoPinsConfigured_IsRefused()
    {
        Assert.False(PinnedHandlerFactory.Accepts(new CertificatePinValidator([]), NewCertificate("server"), SslPolicyErrors.None));
    }

    [Theory]
    [InlineData(SslPolicyErrors.RemoteCertificateChainErrors)]
    [InlineData(SslPolicyErrors.RemoteCertificateNameMismatch)]
    [InlineData(SslPolicyErrors.RemoteCertificateNotAvailable)]
    public void Handler_ChainOrHostNameErrors_AreRefused_EvenWhenThePinMatches(SslPolicyErrors errors)
    {
        var certificate = NewCertificate("server");
        var validator = new CertificatePinValidator([CertificatePinValidator.ComputePin(certificate)]);

        Assert.False(PinnedHandlerFactory.Accepts(validator, certificate, errors));
    }

    [Fact]
    public void Handler_NoCertificate_IsRefused()
    {
        var validator = new CertificatePinValidator(["x"]);

        Assert.False(PinnedHandlerFactory.Accepts(validator, null, SslPolicyErrors.None));
    }

    [Fact]
    public void Factory_CreatesAHandlerWhoseCallbackAppliesTheSameRules()
    {
        var certificate = NewCertificate("server");
        using var handler = PinnedHandlerFactory.Create(new CertificatePinValidator([CertificatePinValidator.ComputePin(certificate)]));

        var callback = handler.ServerCertificateCustomValidationCallback!;

        Assert.True(callback(new HttpRequestMessage(), certificate, null, SslPolicyErrors.None));
        Assert.False(callback(new HttpRequestMessage(), NewCertificate("attacker"), null, SslPolicyErrors.None));
        Assert.False(callback(new HttpRequestMessage(), certificate, null, SslPolicyErrors.RemoteCertificateChainErrors));
    }

    [Fact]
    public void ThirdPartyClient_DoesNotUseThePinnedHandler()
    {
        // Open Food Facts is created over the shared HttpMessageHandler registered by the host, never over PinnedHandlerFactory
        var client = OpenFoodFactsLookup.CreateClient(new HttpClientHandler());

        Assert.Equal(new Uri(OpenFoodFactsLookup.BaseUrl), client.BaseAddress);
        Assert.Contains("PinnedHandlerFactory", File.ReadAllText(SourceFile("Mobile_apps_L1_Model_view", "MauiProgram.cs")));
        Assert.DoesNotContain("PinnedHandlerFactory", File.ReadAllText(SourceFile("StockRoom.Core", "Services/OpenFoodFactsLookup.cs")));
        Assert.DoesNotContain("PinnedHandlerFactory", File.ReadAllText(SourceFile("StockRoom.Core", "ServiceCollectionExtensions.cs")));
    }

    // The source lives in the app project (MauiProgram) or in the Core project (everything else)
    private static string SourceFile(string project, string relative)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, project, relative)))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory?.FullName ?? throw new FileNotFoundException(relative), project, relative);
    }
}
