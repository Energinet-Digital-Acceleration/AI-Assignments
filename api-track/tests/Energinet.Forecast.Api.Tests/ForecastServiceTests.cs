using FluentAssertions;

namespace Energinet.Forecast.Api.Tests;

public class ForecastServiceTests
{
    [Fact]
    public void Generate_ReturnsRequestedNumberOfPoints()
    {
        var svc = new ForecastService();
        svc.Generate("DK1", 24).Should().HaveCount(24);
    }

    [Fact]
    public void Generate_NonNegativeValues()
    {
        var svc = new ForecastService();
        svc.Generate("DK2", 48).Should().OnlyContain(p => p.Mw >= 0);
    }
}