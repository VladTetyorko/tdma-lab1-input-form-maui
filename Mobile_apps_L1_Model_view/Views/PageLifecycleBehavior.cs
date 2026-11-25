using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views;

// Forwards Page.Appearing / Page.Disappearing to the page's ViewModel when it is IPageLifecycleAware
// (DRY: one behavior instead of OnAppearing/OnDisappearing overrides in every code-behind)
public sealed class PageLifecycleBehavior : Behavior<Page>
{
    // The page a confirm sheet lies on: it was not left, so neither event reaches its ViewModel
    private Page? _covered;

    protected override void OnAttachedTo(Page page)
    {
        base.OnAttachedTo(page);
        page.Appearing += OnAppearing;
        page.Disappearing += OnDisappearing;
    }

    protected override void OnDetachingFrom(Page page)
    {
        page.Appearing -= OnAppearing;
        page.Disappearing -= OnDisappearing;
        base.OnDetachingFrom(page);
    }

    private void OnAppearing(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, _covered))
        {
            _covered = null;
            return;
        }

        ((sender as Page)?.BindingContext as IPageLifecycleAware)?.OnAppearing();
    }

    private void OnDisappearing(object? sender, EventArgs e)
    {
        if (MauiConfirmSheet.IsOpen)
        {
            _covered = sender as Page;
            return;
        }

        ((sender as Page)?.BindingContext as IPageLifecycleAware)?.OnDisappearing();
    }
}
