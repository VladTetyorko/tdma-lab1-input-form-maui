using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

// Lab 8: a failing dependency is logged at Error level and the page shows a message instead of crashing
public class LogsViewModelMoqTests
{
    private readonly Mock<ILogReader> _reader = new();
    private readonly Mock<ILogger<LogsViewModel>> _logger = new();
    private readonly LogsViewModel _viewModel;

    public LogsViewModelMoqTests()
    {
        var guard = new Mock<IPermissionGuard>();
        guard.Setup(g => g.CheckAsync(It.IsAny<Func<Role, bool>>(), It.IsAny<string>())).ReturnsAsync((string?)null);
        _viewModel = new LogsViewModel(_reader.Object, guard.Object, _logger.Object);
    }

    [Fact]
    public async Task Refresh_WhenTheReaderThrows_LogsAnErrorWithTheExceptionAndShowsAMessage()
    {
        var failure = new IOException("disk gone");
        _reader.Setup(r => r.ReadNewestAsync(It.IsAny<LogLevel>(), It.IsAny<int>())).ThrowsAsync(failure);

        await _viewModel.RefreshCommand.ExecuteAsync(null);

        _logger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), failure,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        _viewModel.EmptyText.Should().Be("The log files cannot be read.");
        _viewModel.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Refresh_WhenTheReaderWorks_LogsNothingAtErrorLevel()
    {
        _reader.Setup(r => r.ReadNewestAsync(It.IsAny<LogLevel>(), It.IsAny<int>())).ReturnsAsync([]);

        await _viewModel.RefreshCommand.ExecuteAsync(null);

        _logger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
        _viewModel.EmptyText.Should().Be(LogsViewModel.EmptyMessage);
    }
}
