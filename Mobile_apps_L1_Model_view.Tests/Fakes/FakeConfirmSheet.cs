using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// Answers every question with Answer and remembers what was asked
public sealed class FakeConfirmSheet : IConfirmSheet
{
    public bool Answer { get; set; } = true;

    public List<ConfirmRequest> Asked { get; } = [];

    public Task<bool> AskAsync(ConfirmRequest request)
    {
        Asked.Add(request);
        return Task.FromResult(Answer);
    }
}
