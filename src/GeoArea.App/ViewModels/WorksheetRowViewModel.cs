using GeoArea.App.Services;
using GeoArea.Core;

namespace GeoArea.App.ViewModels;

/// <summary>Відформатований рядок відомості для показу в таблиці (крок 3).</summary>
public sealed class WorksheetRowViewModel
{
    public WorksheetRowViewModel(AreaRow row, int decimals)
    {
        Number = row.Number;
        X = NumberFormat.Format(row.X, decimals);
        Y = NumberFormat.Format(row.Y, decimals);
        DeltaX = NumberFormat.Format(row.DeltaX, decimals);
        DeltaY = NumberFormat.Format(row.DeltaY, decimals);
        Product8 = NumberFormat.Format(row.Product8, decimals);
        Product9 = NumberFormat.Format(row.Product9, decimals);
    }

    public int Number { get; }
    public string X { get; }
    public string Y { get; }
    public string DeltaX { get; }
    public string DeltaY { get; }
    public string Product8 { get; }
    public string Product9 { get; }
}
