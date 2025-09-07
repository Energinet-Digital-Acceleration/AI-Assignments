using Energinet.DataTransform.Console;
using FluentAssertions;

namespace Energinet.DataTransform.Tests;

public class TopPriceCalculatorTests
{
    [Fact]
    public void GetTopN_ReturnsTop3Descending()
    {
        // Arrange: 24 prices 0..23 as decimals
        var prices = Enumerable.Range(0,24).Select(i => (decimal)i).ToList();
        // Act
        var result = TopPriceCalculator.GetTopN(prices, 3);
        // Assert
        result.Should().HaveCount(3);
        result[0].Hour.Should().Be(23);
        result[0].Price.Should().Be(23m);
        result[1].Hour.Should().Be(22);
        result[1].Price.Should().Be(22m);
        result[2].Hour.Should().Be(21);
        result[2].Price.Should().Be(21m);
    }

    [Fact]
    public void GetTopN_HandlesPriceTies_ByEarlierHour()
    {
        var prices = Enumerable.Repeat(1m,24).ToList();
        var result = TopPriceCalculator.GetTopN(prices,3);
        result.Select(r=>r.Hour).Should().ContainInOrder(0,1,2);
    }

    [Fact]
    public void GetTopN_Throws_WhenNot24()
    {
        var prices = new List<decimal>{1m};
        Action act = () => TopPriceCalculator.GetTopN(prices,3);
        act.Should().Throw<ArgumentException>();
    }
}
