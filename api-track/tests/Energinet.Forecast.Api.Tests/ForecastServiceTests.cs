using FluentAssertions;

namespace Energinet.Forecast.Api.Tests;

public class ForecastServiceTests
{
    [Fact]
    public void VigtigTest()
    {
        var forventet = "Forventet værdi";
        forventet.Should().Be("Forventet værdi");
    }
}