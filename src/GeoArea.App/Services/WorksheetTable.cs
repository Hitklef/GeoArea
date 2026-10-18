using GeoArea.Core;

namespace GeoArea.App.Services;

/// <summary>
/// Відомість у вигляді готових текстових комірок за бланком (9 стовпців):
/// № | ± | X | ± | Y | X(n−1)−X(n+1) | Y(n+1)−Y(n−1) | (X(n−1)−X(n+1))·Y(n) | (Y(n+1)−Y(n−1))·X(n).
/// Використовується для копіювання, збереження у Word і друку, щоб усі три виводи були однакові.
/// </summary>
public sealed class WorksheetTable
{
    public const string Title = "Відомість обчислення площі по координатам точок вершин";

    public static readonly string[] FlatHeaders =
    [
        "№ п/п", "±", "X", "±", "Y",
        "Xn-1 − Xn+1", "Yn+1 − Yn-1",
        "(Xn-1 − Xn+1)·Yn", "(Yn+1 − Yn-1)·Xn"
    ];

    public static readonly string[] ColumnNumbers = ["1", "2", "3", "4", "5", "6", "7", "8", "9"];

    private WorksheetTable(IReadOnlyList<string[]> rows, string[] sumRow, string doubleArea, string areaM2, string areaHa, bool controlPassed, IReadOnlyList<string> warnings)
    {
        Rows = rows;
        SumRow = sumRow;
        DoubleAreaText = doubleArea;
        AreaSquareMetersText = areaM2;
        AreaHectaresText = areaHa;
        ControlPassed = controlPassed;
        Warnings = warnings;
    }

    /// <summary>Рядки вершин, по 9 комірок у кожному.</summary>
    public IReadOnlyList<string[]> Rows { get; }

    /// <summary>Рядок «Сума»: 9 комірок, перші п'ять — підпис і порожні.</summary>
    public string[] SumRow { get; }

    public string DoubleAreaText { get; }
    public string AreaSquareMetersText { get; }
    public string AreaHectaresText { get; }
    public bool ControlPassed { get; }
    public IReadOnlyList<string> Warnings { get; }

    public string AreaLine => $"P = {AreaSquareMetersText} м² = {AreaHectaresText} га";

    public static WorksheetTable Build(AreaResult result, int decimals)
    {
        var rows = new List<string[]>(result.Rows.Count);
        foreach (var r in result.Rows)
        {
            rows.Add(
            [
                r.Number.ToString(),
                Sign(r.X),
                NumberFormat.Format(Math.Abs(r.X), decimals),
                Sign(r.Y),
                NumberFormat.Format(Math.Abs(r.Y), decimals),
                NumberFormat.Format(r.DeltaX, decimals),
                NumberFormat.Format(r.DeltaY, decimals),
                NumberFormat.Format(r.Product8, decimals),
                NumberFormat.Format(r.Product9, decimals)
            ]);
        }

        string[] sum =
        [
            "Сума", "", "", "", "",
            NumberFormat.Format(result.SumDeltaX, decimals),
            NumberFormat.Format(result.SumDeltaY, decimals),
            NumberFormat.Format(result.Sum8, decimals),
            NumberFormat.Format(result.Sum9, decimals)
        ];

        return new WorksheetTable(
            rows,
            sum,
            NumberFormat.Format(result.DoubleArea, decimals),
            NumberFormat.Format(result.AreaSquareMeters, 2),
            NumberFormat.Format(result.AreaHectares, 4),
            result.ControlPassed,
            result.Warnings);
    }

    private static string Sign(double v) => v < 0 ? "−" : "+";
}
