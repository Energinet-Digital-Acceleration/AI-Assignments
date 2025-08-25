using Energinet.DataTransform.Console;
using FluentAssertions;

namespace Energinet.DataTransform.Tests;

public class RollingAverageTests
{
    [Fact]
    public void Computes_SimpleAverage()
    {
        var rows = Enumerable.Range(0, 5)
            .Select(i => new Row(DateTimeOffset.Now.AddHours(i), i + 1))
            .ToList();

        var result = RollingAverage.Add(rows, window: 2).ToList();
        result.Last().rolling.Should().Be(4.5);
    }
}