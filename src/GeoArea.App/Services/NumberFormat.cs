using System.Globalization;

namespace GeoArea.App.Services;

/// <summary>Єдине місце форматування чисел: український формат «5 432,10», без «мінус нуля».</summary>
public static class NumberFormat
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("uk-UA");

    public static string Format(double value, int decimals)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) return "—";

        // Значення на кшталт -0,00 показуємо як 0,00.
        double half = 0.5 * Math.Pow(10, -decimals);
        if (Math.Abs(value) < half) value = 0;

        return value.ToString("N" + decimals, Culture);
    }

    /// <summary>Формат для вставки в Excel/Word: з роздільником розрядів-пробілом це теж читається, але надійніше без нього.</summary>
    public static string FormatPlain(double value, int decimals)
    {
        double half = 0.5 * Math.Pow(10, -decimals);
        if (Math.Abs(value) < half) value = 0;
        return value.ToString("F" + decimals, Culture);
    }
}
