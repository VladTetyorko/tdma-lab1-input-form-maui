using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

public sealed class FakeFileSharer : IFileSharer
{
    public List<(string Title, string FilePath)> Shared { get; } = [];

    public Task ShareFileAsync(string title, string filePath)
    {
        Shared.Add((title, filePath));
        return Task.CompletedTask;
    }
}
