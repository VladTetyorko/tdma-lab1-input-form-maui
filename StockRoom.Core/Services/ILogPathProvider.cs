namespace Mobile_apps_L1_Model_view.Services;

// Where the log files live. A platform abstraction so tests can use a temporary folder
public interface ILogPathProvider
{
    // The folder that holds stockroom-YYYYMMDD.log files (created by the file sink)
    string LogDirectory { get; }
}
