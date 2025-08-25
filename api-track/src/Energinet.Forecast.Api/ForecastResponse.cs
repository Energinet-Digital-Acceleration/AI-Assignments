namespace Energinet.Forecast.Api;

public record ForecastResponse(string Area, IReadOnlyList<ForecastPoint> Values);