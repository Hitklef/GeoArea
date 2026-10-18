using GeoArea.Core;

namespace GeoArea.Core.Tests;

public class AreaCalculatorTests
{
    private static readonly Vertex[] Square =
    [
        new(0, 0), new(0, 10), new(10, 10), new(10, 0)
    ];

    [Fact]
    public void Square_10x10_HasArea100()
    {
        var result = AreaCalculator.Calculate(Square);

        Assert.Equal(100, result.AreaSquareMeters, 9);
        Assert.Equal(0.01, result.AreaHectares, 9);
        Assert.Equal(200, result.DoubleArea, 9);
        Assert.True(result.ControlPassed);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Triangle_3_4_HasArea6()
    {
        Vertex[] triangle = [new(0, 0), new(4, 0), new(0, 3)];

        var result = AreaCalculator.Calculate(triangle);

        Assert.Equal(6, result.AreaSquareMeters, 9);
        Assert.True(result.ControlPassed);
    }

    [Fact]
    public void ReverseOrder_GivesSameArea_OppositeSign()
    {
        var forward = AreaCalculator.Calculate(Square);
        var backward = AreaCalculator.Calculate(Square.Reverse().ToArray());

        Assert.Equal(forward.AreaSquareMeters, backward.AreaSquareMeters, 9);
        Assert.Equal(-forward.Sum8, backward.Sum8, 9);
        Assert.NotEqual(forward.IsClockwise, backward.IsClockwise);
    }

    [Fact]
    public void NegativeCoordinates_AreHandled()
    {
        Vertex[] shifted = Square.Select(v => new Vertex(v.X - 100, v.Y - 50)).ToArray();

        var result = AreaCalculator.Calculate(shifted);

        Assert.Equal(100, result.AreaSquareMeters, 9);
        Assert.True(result.ControlPassed);
    }

    [Fact]
    public void Rows_FollowWorksheetColumns()
    {
        // Вершини: 1(0,0) 2(0,10) 3(10,10) 4(10,0)
        var result = AreaCalculator.Calculate(Square);
        var r1 = result.Rows[0];

        // Для точки 1: попередня — 4 (10,0), наступна — 2 (0,10)
        Assert.Equal(1, r1.Number);
        Assert.Equal(10 - 0, r1.DeltaX, 9);   // X4 − X2
        Assert.Equal(10 - 0, r1.DeltaY, 9);   // Y2 − Y4
        Assert.Equal(r1.DeltaX * r1.Y, r1.Product8, 9);
        Assert.Equal(r1.DeltaY * r1.X, r1.Product9, 9);

        Assert.Equal(0, result.SumDeltaX, 9);
        Assert.Equal(0, result.SumDeltaY, 9);
        Assert.Equal(result.Sum8, result.Sum9, 9);
    }

    [Fact]
    public void ClosedContour_LastPointEqualsFirst_IsNotCountedTwice()
    {
        Vertex[] closed = [.. Square, Square[0]];

        var result = AreaCalculator.Calculate(closed);

        Assert.Equal(4, result.Rows.Count);
        Assert.Equal(100, result.AreaSquareMeters, 9);
        Assert.Contains(result.Warnings, w => w.Contains("замкнений", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ClosedContour_CanBeKept_WhenOptionDisabled()
    {
        Vertex[] closed = [.. Square, Square[0]];

        var result = AreaCalculator.Calculate(closed, new AreaOptions(IgnoreClosingDuplicate: false));

        Assert.Equal(5, result.Rows.Count);
        // Дублікат на нульовому ребрі площу не змінює.
        Assert.Equal(100, result.AreaSquareMeters, 9);
    }

    [Fact]
    public void ConsecutiveDuplicate_IsSkippedWithWarning()
    {
        Vertex[] withDuplicate = [new(0, 0), new(0, 10), new(0, 10), new(10, 10), new(10, 0)];

        var result = AreaCalculator.Calculate(withDuplicate);

        Assert.Equal(4, result.Rows.Count);
        Assert.Equal(100, result.AreaSquareMeters, 9);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void SelfIntersecting_Bowtie_ProducesWarning()
    {
        Vertex[] bowtie = [new(0, 0), new(10, 10), new(10, 0), new(0, 10)];

        var result = AreaCalculator.Calculate(bowtie);

        Assert.Contains(result.Warnings, w => w.Contains("перетинає", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Collinear_Points_ProduceZeroAreaWarning()
    {
        Vertex[] line = [new(0, 0), new(5, 5), new(10, 10)];

        var result = AreaCalculator.Calculate(line);

        Assert.Equal(0, result.AreaSquareMeters, 9);
        Assert.Contains(result.Warnings, w => w.Contains("нулю", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FewerThanThreeVertices_Throws()
    {
        Vertex[] two = [new(0, 0), new(1, 1)];

        Assert.Throws<ArgumentException>(() => AreaCalculator.Calculate(two));
    }

    [Fact]
    public void RealWorldCoordinates_ControlPasses()
    {
        // Типові геодезичні координати: великі числа, два знаки після коми.
        Vertex[] parcel =
        [
            new(5432.10, 1234.56),
            new(5498.75, 1301.20),
            new(5456.30, 1389.45),
            new(5390.85, 1322.80)
        ];

        var result = AreaCalculator.Calculate(parcel);

        Assert.True(result.ControlPassed);
        Assert.True(result.AreaSquareMeters > 0);
        Assert.Equal(result.Sum8, result.Sum9, 3);
    }
}
