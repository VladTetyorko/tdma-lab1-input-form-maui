using System.Net;
using System.Text;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// The network as the test wants it: a canned answer or an exception. By default there is no network
// (HttpRequestException), so a test that forgets to set it up can never reach the real internet.
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private Func<HttpRequestMessage, HttpResponseMessage> _responder =
        _ => throw new HttpRequestException("No network in tests.");

    public List<HttpRequestMessage> Requests { get; } = [];

    public void RespondWith(HttpStatusCode status, string body) =>
        _responder = _ => new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    public void Throw(Exception exception) => _responder = _ => throw exception;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        try
        {
            return Task.FromResult(_responder(request));
        }
        catch (Exception ex)
        {
            return Task.FromException<HttpResponseMessage>(ex);
        }
    }
}
