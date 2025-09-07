using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Energinet.Forecast.Api.Tests;

public class GetForecastsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GetForecastsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GET_forecasts_returns_200_and_empty_array_initially()
    {
        var response = await _client.GetAsync("/forecasts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<List<ForecastDto>>();
        payload.Should().NotBeNull();
        payload!.Should().BeEmpty();
    }

    private record ForecastDto(string Date, decimal Value);
}
