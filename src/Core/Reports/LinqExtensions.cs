namespace Core.Reports;

// Додаткове завдання 2: власний метод-розширення для топ-N з детермінованим порядком.
public static class LinqExtensions
{
    public static IReadOnlyList<T> TopBy<T>(
        this IEnumerable<T> source,
        Func<T, decimal> primary,
        Func<T, string> secondary,
        int n) =>
        source.OrderByDescending(primary)
              .ThenBy(secondary, StringComparer.Ordinal)
              .Take(n)
              .ToList();
}
