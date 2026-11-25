namespace Mobile_apps_L1_Model_view.Services;

// One value of a row, with the width and alignment of its column
public sealed record ProductTableCell(string Text, double Width, bool IsNumeric);
