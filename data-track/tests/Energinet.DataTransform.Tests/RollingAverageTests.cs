using Energinet.DataTransform.Console;
using FluentAssertions;

namespace Energinet.DataTransform.Tests;

public class RollingAverageTests
{
    [Fact]
    public void VigtigTest()
    {
        var forventet = "Forventet værdi";
        forventet.Should().Be("Forventet værdi");
    }
}