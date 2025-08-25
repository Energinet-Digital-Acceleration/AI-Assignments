namespace Energinet.Forecast.Api;

public interface IForecastService { IReadOnlyList<ForecastPoint> Generate(string area, int hours); }