using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Views;

// View of one ConfirmRequest; Answer completes once
public partial class ConfirmSheetPage : ContentPage
{
    private readonly TaskCompletionSource<bool> _answer = new();

    public ConfirmSheetPage(ConfirmRequest request)
    {
        InitializeComponent();
        TitleText.Text = request.Title;
        MessageText.Text = request.Message;
        foreach (var line in request.Lines)
        {
            LineList.Add(new Label { Text = line, Style = (Style)Application.Current!.Resources["TextBodyStrong"] });
        }

        AcceptButton.Text = request.Accept;
        AcceptButton.StyleClass = [request.IsDestructive ? "Danger" : "Primary"];
        CancelButton.Text = request.Cancel;
        SetDynamicResource(BackgroundColorProperty, "Overlay");
    }

    public Task<bool> Answer => _answer.Task;

    protected override bool OnBackButtonPressed()
    {
        _answer.TrySetResult(false);
        return true;
    }

    private void OnAccept(object? sender, EventArgs e) => _answer.TrySetResult(true);

    private void OnCancel(object? sender, EventArgs e) => _answer.TrySetResult(false);
}
