using System.Collections;
using System.Windows.Input;

namespace Mobile_apps_L1_Model_view.Views.Components;

public partial class OperationTable : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource),
        typeof(IEnumerable), typeof(OperationTable),
        propertyChanged: (table, _, value) => BindableLayout.SetItemsSource(((OperationTable)table).Days, (IEnumerable?)value));

    // Runs with the tapped OperationRowItem
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(OperationTable));

    public OperationTable()
    {
        InitializeComponent();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
}
