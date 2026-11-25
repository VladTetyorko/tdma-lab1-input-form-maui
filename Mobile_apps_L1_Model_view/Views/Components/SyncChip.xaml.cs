using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views.Components;

public partial class SyncChip : SessionView
{
    public SyncChip()
    {
        InitializeComponent();
    }

    // On a page title row the chip is shown only when something is not synced; the drawer shows it always
    public bool OnlyWhenNeeded { get; set; }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (OnlyWhenNeeded)
        {
            Content?.SetBinding(IsVisibleProperty, static (SessionStatusViewModel status) => status.Connectivity.NeedsAttention);
        }
    }
}
