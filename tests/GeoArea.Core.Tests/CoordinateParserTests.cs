using GeoArea.Core;

namespace GeoArea.Core.Tests;

public class CoordinateParserTests
{
    [Theory]
    [InlineData("1,5 2,5")]
    [InlineData("1.5\t2.5")]
    [InlineData("1,5;2,5")]
    [InlineData("1.5, 2.5")]
    [InlineData("  1,5   2,5  ")]
    public void Parses_TwoNumbers_WithAnySeparator(string line)
    {
        var result = CoordinateParser.Parse(line);

        var v = Assert.Single(result.Vertices);
        Assert.Equal(1.5, v.X);
        Assert.Equal(2.5, v.Y);
        Assert.Empty(result.SkippedLines);
    }

    [Fact]
    public void Parses_NumberedRows_TakingLastTwoValues()
    {
        var result = CoordinateParser.Parse("1\t5432,10\t1234,56\r\n2\t5498,75\t1301,20\r\n");

        Assert.Equal(2, result.Vertices.Count);
        Assert.Equal(new Vertex(5432.10, 1234.56), result.Vertices[0]);
        Assert.Equal(new Vertex(5498.75, 1301.20), result.Vertices[1]);
    }

    [Fact]
    public void Skips_HeaderAndEmptyLines()
    {
        var text = "№\tX\tY\n\n1\t10\t20\nкінець\n";

        var result = CoordinateParser.Parse(text);

        Assert.Single(result.Vertices);
        Assert.Equal(2, result.SkippedLines.Count);
    }

    [Fact]
    public void Parses_NegativeNumbers()
    {
        var result = CoordinateParser.Parse("-10,5 -20");

        var v = Assert.Single(result.Vertices);
        Assert.Equal(-10.5, v.X);
        Assert.Equal(-20, v.Y);
    }

    [Fact]
    public void Empty_Input_GivesNothing()
    {
        Assert.False(CoordinateParser.Parse(null).HasVertices);
        Assert.False(CoordinateParser.Parse("   ").HasVertices);
    }

    [Theory]
    [InlineData("12,5", 12.5)]
    [InlineData("12.5", 12.5)]
    [InlineData("-3", -3)]
    [InlineData("5 432,10", 5432.10)]
    public void TryParseNumber_AcceptsCommaAndDot(string text, double expected)
    {
        Assert.True(CoordinateParser.TryParseNumber(text, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1,2,3")]
    public void TryParseNumber_RejectsGarbage(string text)
    {
        Assert.False(CoordinateParser.TryParseNumber(text, out _));
    }
}
