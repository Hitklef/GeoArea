namespace GeoArea.Core;

/// <summary>
/// Один рядок «Відомості обчислення площі по координатам точок вершин».
/// Номери стовпців відповідають бланку: 1 — №, 3 — X, 5 — Y, 6 — ΔX, 7 — ΔY, 8 і 9 — добутки.
/// </summary>
/// <param name="Number">Порядковий номер вершини (стовпець 1).</param>
/// <param name="X">Координата X (стовпець 3).</param>
/// <param name="Y">Координата Y (стовпець 5).</param>
/// <param name="DeltaX">X(n−1) − X(n+1) (стовпець 6).</param>
/// <param name="DeltaY">Y(n+1) − Y(n−1) (стовпець 7).</param>
/// <param name="Product8">(X(n−1) − X(n+1)) · Y(n) (стовпець 8).</param>
/// <param name="Product9">(Y(n+1) − Y(n−1)) · X(n) (стовпець 9).</param>
public sealed record AreaRow(
    int Number,
    double X,
    double Y,
    double DeltaX,
    double DeltaY,
    double Product8,
    double Product9);
