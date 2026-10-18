using System.Globalization;
using System.Text.RegularExpressions;

namespace GeoArea.Core;

/// <summary>Результат розбору тексту з координатами.</summary>
/// <param name="Vertices">Розпізнані вершини в порядку появи.</param>
/// <param name="SkippedLines">Непорожні рядки, в яких не вдалося знайти пару чисел (для повідомлення користувачу).</param>
public sealed record ParseResult(IReadOnlyList<Vertex> Vertices, IReadOnlyList<string> SkippedLines)
{
    public bool HasVertices => Vertices.Count > 0;
}

/// <summary>
/// Розбирає координати, вставлені з буфера обміну або прочитані з текстового файлу.
/// Підтримує роздільники: пробіл, табуляція, кома, крапка з комою; десятковий роздільник — кома або крапка.
/// Якщо в рядку три і більше чисел, перше вважається номером точки, а X та Y — два останні.
/// </summary>
public static partial class CoordinateParser
{
    [GeneratedRegex(@"[-−]?\d+(?:[.,]\d+)?", RegexOptions.CultureInvariant)]
    private static partial Regex NumberRegex();

    public static ParseResult Parse(string? text)
    {
        var vertices = new List<Vertex>();
        var skipped = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ParseResult(vertices, skipped);
        }

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim().TrimEnd('\r');
            if (line.Length == 0) continue;

            var numbers = NumberRegex().Matches(line).Select(m => m.Value).ToList();
            if (numbers.Count < 2)
            {
                skipped.Add(line);
                continue;
            }

            // Якщо чисел три і більше — перше (або перші) це номер точки, беремо два останні.
            string xs = numbers[^2];
            string ys = numbers[^1];

            if (TryParseNumber(xs, out double x) && TryParseNumber(ys, out double y))
            {
                vertices.Add(new Vertex(x, y));
            }
            else
            {
                skipped.Add(line);
            }
        }

        return new ParseResult(vertices, skipped);
    }

    /// <summary>Розбирає одне число: приймає кому або крапку як десятковий роздільник і пробіли між розрядами.</summary>
    public static bool TryParseNumber(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var cleaned = text.Trim()
            .Replace("−", "-")          // типографський мінус
            .Replace(" ", string.Empty)  // пробіл між розрядами
            .Replace(" ", string.Empty)
            .Replace(',', '.');

        return double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
