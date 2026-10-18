namespace GeoArea.Core;

/// <summary>Налаштування розрахунку.</summary>
/// <param name="IgnoreClosingDuplicate">
/// Якщо остання вершина збігається з першою (контур «замкнули» вручну), не враховувати її повторно.
/// </param>
/// <param name="Tolerance">Допуск, у межах якого два числа вважаються рівними (для контролю та порівняння точок).</param>
public sealed record AreaOptions(bool IgnoreClosingDuplicate = true, double Tolerance = 1e-6)
{
    public static AreaOptions Default { get; } = new();
}
