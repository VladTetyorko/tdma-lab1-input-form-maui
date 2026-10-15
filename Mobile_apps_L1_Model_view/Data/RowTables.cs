using System.Reflection;
using Mobile_apps_L1_Model_view.Data.Rows;
using SQLite;

namespace Mobile_apps_L1_Model_view.Data;

// Table name of a row type (its [Table] attribute)
public static class RowTables
{
    public static string NameOf(Type rowType) =>
        rowType.GetCustomAttribute<TableAttribute>()?.Name
        ?? throw new InvalidOperationException($"{rowType.Name} has no [Table] attribute.");

    public static string NameOf<TRow>() where TRow : Row => NameOf(typeof(TRow));
}
