namespace GeoArea.Core;

/// <summary>Вершина полігона: координати X та Y у метрах.</summary>
public readonly record struct Vertex(double X, double Y)
{
    public override string ToString() => $"({X}; {Y})";
}
