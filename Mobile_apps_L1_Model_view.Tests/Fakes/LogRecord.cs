using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// One captured log call: the level, the category (class name), the rendered message and the exception
public sealed record LogRecord(LogLevel Level, string Category, string Message, Exception? Exception);
