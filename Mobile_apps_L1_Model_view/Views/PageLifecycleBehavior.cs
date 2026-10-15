using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views;

// Forwards Page.Appearing / Page.Disappearing to the page's ViewModel when it is IPageLifecycleAware
// (DRY: one behavior instead of OnAppearing/OnDisappearing overrides in every code-behind)
public sealed class PageLifecycleBehavior : Behavior<Page>
{
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

    private static void OnAppearing(object? sender, EventArgs e) =>
        ((sender as Page)?.BindingContext as IPageLifecycleAware)?.OnAppearing();

    private static void OnDisappearing(object? sender, EventArgs e) =>
        ((sender as Page)?.BindingContext as IPageLifecycleAware)?.OnDisappearing();
}
