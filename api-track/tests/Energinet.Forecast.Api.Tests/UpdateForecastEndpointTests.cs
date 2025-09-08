using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Energinet.Forecast.Api.Tests;

public class UpdateForecastEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UpdateForecastEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PUT_existing_forecast_returns_200_and_updates_value()
    {
        // Arrange: get existing forecasts
        var list = await _client.GetFromJsonAsync<List<ForecastDto>>("/forecasts");
        list.Should().NotBeNull();
        list!.Count.Should().BeGreaterThan(0);
        var first = list[0];

        var newValue = first.Value + 42;
        var response = await _client.PutAsJsonAsync($"/forecasts/{first.Date}", new { value = newValue });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<ForecastDto>();
        updated.Should().NotBeNull();
        updated!.Value.Should().Be(newValue);

        // Confirm persisted in list
        var listAfter = await _client.GetFromJsonAsync<List<ForecastDto>>("/forecasts");
        listAfter!.First(f => f.Date == first.Date).Value.Should().Be(newValue);
    }

    [Fact]
    public async Task PUT_non_existing_forecast_returns_404()
    {
        var missingDate = DateTime.UtcNow.Date.AddDays(30).ToString("yyyy-MM-dd");
        var response = await _client.PutAsJsonAsync($"/forecasts/{missingDate}", new { value = 999m });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_invalid_date_returns_400()
    {
        var response = await _client.PutAsJsonAsync("/forecasts/not-a-date", new { value = 10m });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private record ForecastDto(string Date, decimal Value);
}
