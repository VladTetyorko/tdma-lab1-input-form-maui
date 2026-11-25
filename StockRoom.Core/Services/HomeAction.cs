namespace Mobile_apps_L1_Model_view.Services;

// One button of the Home page: where it goes and what it says
public sealed record HomeAction(string Route, string Title, string Hint = "");
