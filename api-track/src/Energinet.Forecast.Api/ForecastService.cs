namespace Energinet.Forecast.Api;

public class ForecastService : IForecastService
{
    public IReadOnlyList<ForecastPoint> Generate(string area, int hours)
    {
        var now = DateTimeOffset.UtcNow;
        var random = new Random(area.GetHashCode());
        var forecastPoints = new List<ForecastPoint>(hours);
        var baseLoad = area == "DK1" ? 1500 : 1200;
        for (var hour = 0; hour < hours; hour++)
        {
            var ts = now.AddHours(hour);
            var dailyWave = Math.Sin(ts.Hour / 24.0 * 2 * Math.PI) * 200;
            var noise = random.NextDouble() * 50 - 25;
            var mw = Math.Max(0, baseLoad + dailyWave + noise);
            forecastPoints.Add(new ForecastPoint(ts, Math.Round(mw, 2)));
        }
        return forecastPoints;
    }
}