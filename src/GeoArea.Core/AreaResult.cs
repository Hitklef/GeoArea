namespace GeoArea.Core;

/// <summary>Результат обчислення площі: заповнена відомість, суми, контроль і площа.</summary>
public sealed class AreaResult
{
    public AreaResult(
        IReadOnlyList<AreaRow> rows,
        double sumDeltaX,
        double sumDeltaY,
        double sum8,
        double sum9,
        bool controlPassed,
        bool isClockwise,
        IReadOnlyList<string> warnings)
    {
        Rows = rows;
        SumDeltaX = sumDeltaX;
        SumDeltaY = sumDeltaY;
        Sum8 = sum8;
        Sum9 = sum9;
        ControlPassed = controlPassed;
        IsClockwise = isClockwise;
        Warnings = warnings;
    }

    /// <summary>Рядки відомості — по одному на вершину.</summary>
    public IReadOnlyList<AreaRow> Rows { get; }

    /// <summary>Сума стовпця 6. Контроль: має дорівнювати нулю.</summary>
    public double SumDeltaX { get; }

    /// <summary>Сума стовпця 7. Контроль: має дорівнювати нулю.</summary>
    public double SumDeltaY { get; }

    /// <summary>Сума стовпця 8 = 2P (зі знаком, що залежить від напрямку обходу).</summary>
    public double Sum8 { get; }

    /// <summary>Сума стовпця 9 = 2P. Контроль: має збігатися із сумою стовпця 8.</summary>
    public double Sum9 { get; }

    /// <summary>Подвоєна площа 2P, м² (завжди додатна).</summary>
    public double DoubleArea => Math.Abs(Sum8);

    /// <summary>Площа P, м².</summary>
    public double AreaSquareMeters => DoubleArea / 2.0;

    /// <summary>Площа P, га (1 га = 10 000 м²).</summary>
    public double AreaHectares => AreaSquareMeters / 10_000.0;

    /// <summary>Чи пройшли всі три контролі (Σ6 = 0, Σ7 = 0, Σ8 = Σ9).</summary>
    public bool ControlPassed { get; }

    /// <summary>Напрямок обходу вершин: true — за годинниковою стрілкою (у математичній системі координат).</summary>
    public bool IsClockwise { get; }

    /// <summary>Попередження, які не заважають рахунку, але на які варто звернути увагу користувача.</summary>
    public IReadOnlyList<string> Warnings { get; }
}
