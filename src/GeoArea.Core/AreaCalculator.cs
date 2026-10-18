namespace GeoArea.Core;

/// <summary>
/// Обчислення площі полігона за координатами вершин формулою Гаусса:
/// 2P = Σ Yn·(Xn−1 − Xn+1) = Σ Xn·(Yn+1 − Yn−1).
/// Індекси циклічні: для першої вершини «попередня» — остання, для останньої «наступна» — перша.
/// </summary>
public static class AreaCalculator
{
    /// <summary>Мінімальна кількість вершин, потрібна для розрахунку.</summary>
    public const int MinimumVertices = 3;

    /// <summary>
    /// Розраховує відомість і площу.
    /// </summary>
    /// <exception cref="ArgumentException">Менше трьох різних вершин.</exception>
    public static AreaResult Calculate(IReadOnlyList<Vertex> vertices, AreaOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        options ??= AreaOptions.Default;

        var warnings = new List<string>();
        var points = PrepareVertices(vertices, options, warnings);

        if (points.Count < MinimumVertices)
        {
            throw new ArgumentException(
                $"Для розрахунку потрібно щонайменше {MinimumVertices} вершини, отримано {points.Count}.",
                nameof(vertices));
        }

        int n = points.Count;
        var rows = new List<AreaRow>(n);
        double sumDx = 0, sumDy = 0, sum8 = 0, sum9 = 0;

        for (int i = 0; i < n; i++)
        {
            var prev = points[(i - 1 + n) % n];
            var cur = points[i];
            var next = points[(i + 1) % n];

            double dx = prev.X - next.X;      // стовпець 6
            double dy = next.Y - prev.Y;      // стовпець 7
            double p8 = dx * cur.Y;           // стовпець 8
            double p9 = dy * cur.X;           // стовпець 9

            sumDx += dx;
            sumDy += dy;
            sum8 += p8;
            sum9 += p9;

            rows.Add(new AreaRow(i + 1, cur.X, cur.Y, dx, dy, p8, p9));
        }

        // Допуск для контролю масштабуємо під величину чисел: добутки координат порядку 10^6–10^9
        // накопичують похибку подвійної точності, яка не є помилкою обчислення.
        double scale = Math.Max(1.0, Math.Max(Math.Abs(sum8), Math.Abs(sum9)));
        double tol = Math.Max(options.Tolerance, scale * 1e-9);
        bool control =
            Math.Abs(sumDx) <= tol &&
            Math.Abs(sumDy) <= tol &&
            Math.Abs(sum8 - sum9) <= tol;

        if (Math.Abs(sum8) <= tol)
        {
            warnings.Add("Площа дорівнює нулю: усі точки лежать на одній прямій або контур вироджений.");
        }

        if (HasSelfIntersection(points))
        {
            warnings.Add("Межа ділянки перетинає сама себе. Перевірте порядок точок — площа такого контуру неправильна.");
        }

        // У математичній системі координат додатна сума — обхід проти годинникової стрілки.
        bool clockwise = sum8 < 0;

        return new AreaResult(rows, sumDx, sumDy, sum8, sum9, control, clockwise, warnings);
    }

    /// <summary>Повертає робочий список вершин: без замикаючого дубліката та без підряд повторених точок.</summary>
    private static List<Vertex> PrepareVertices(IReadOnlyList<Vertex> source, AreaOptions options, List<string> warnings)
    {
        var points = new List<Vertex>(source.Count);
        foreach (var v in source)
        {
            if (double.IsNaN(v.X) || double.IsNaN(v.Y) || double.IsInfinity(v.X) || double.IsInfinity(v.Y))
            {
                throw new ArgumentException("Координати містять недопустиме число.", nameof(source));
            }

            if (points.Count > 0 && AreSame(points[^1], v, options.Tolerance))
            {
                warnings.Add($"Точка {points.Count + 1} повторює попередню і не враховується.");
                continue;
            }

            points.Add(v);
        }

        if (options.IgnoreClosingDuplicate && points.Count > MinimumVertices && AreSame(points[0], points[^1], options.Tolerance))
        {
            points.RemoveAt(points.Count - 1);
            warnings.Add("Остання точка збігається з першою (замкнений контур) — вона не рахується двічі.");
        }

        return points;
    }

    private static bool AreSame(Vertex a, Vertex b, double tol) =>
        Math.Abs(a.X - b.X) <= tol && Math.Abs(a.Y - b.Y) <= tol;

    /// <summary>Перевірка самоперетину: чи перетинаються будь-які два несуміжні ребра.</summary>
    public static bool HasSelfIntersection(IReadOnlyList<Vertex> points)
    {
        int n = points.Count;
        if (n < 4) return false;

        for (int i = 0; i < n; i++)
        {
            var a1 = points[i];
            var a2 = points[(i + 1) % n];
            for (int j = i + 1; j < n; j++)
            {
                // суміжні ребра (мають спільну вершину) не перевіряємо
                if (j == i + 1 || (i == 0 && j == n - 1)) continue;

                var b1 = points[j];
                var b2 = points[(j + 1) % n];
                if (SegmentsIntersect(a1, a2, b1, b2)) return true;
            }
        }

        return false;
    }

    private static bool SegmentsIntersect(Vertex p1, Vertex p2, Vertex p3, Vertex p4)
    {
        double d1 = Cross(p3, p4, p1);
        double d2 = Cross(p3, p4, p2);
        double d3 = Cross(p1, p2, p3);
        double d4 = Cross(p1, p2, p4);

        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
        {
            return true;
        }

        if (d1 == 0 && OnSegment(p3, p4, p1)) return true;
        if (d2 == 0 && OnSegment(p3, p4, p2)) return true;
        if (d3 == 0 && OnSegment(p1, p2, p3)) return true;
        if (d4 == 0 && OnSegment(p1, p2, p4)) return true;
        return false;
    }

    private static double Cross(Vertex a, Vertex b, Vertex c) =>
        (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

    private static bool OnSegment(Vertex a, Vertex b, Vertex c) =>
        Math.Min(a.X, b.X) <= c.X && c.X <= Math.Max(a.X, b.X) &&
        Math.Min(a.Y, b.Y) <= c.Y && c.Y <= Math.Max(a.Y, b.Y);
}
