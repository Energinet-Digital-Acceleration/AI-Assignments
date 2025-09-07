namespace Energinet.DataTransform.Console;

public static class TopPriceCalculator
{
    public static IReadOnlyList<(int Hour, decimal Price)> GetTopN(IReadOnlyList<decimal> hourlyPrices, int n = 3)
    {
        if (hourlyPrices is null) throw new ArgumentNullException(nameof(hourlyPrices));
        if (hourlyPrices.Count != 24) throw new ArgumentException("Hourly prices must contain exactly 24 values (one per hour).", nameof(hourlyPrices));
        if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n));

        return hourlyPrices
            .Select((price, hour) => (Hour: hour, Price: price))
            .OrderByDescending(p => p.Price)
            .ThenBy(p => p.Hour) // deterministic order when prices tie
            .Take(n)
            .ToList();
    }
}
