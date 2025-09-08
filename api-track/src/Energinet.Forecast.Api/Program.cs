using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.TypedResults;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register simple in-memory forecast store (seeded with demo data)
builder.Services.AddSingleton<ForecastStore>();

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

// Health endpoint (anonymous) returning a simple JSON payload { status = "Healthy" }
app.MapGet("/health", () => Ok(new { status = "Healthy" }))
   .WithName("Health")
   .WithOpenApi();

// GET /forecasts returns the current list of forecasts (can be empty)
app.MapGet("/forecasts", (ForecastStore store) => Ok(store.Forecasts))
   .WithName("GetForecasts")
   .WithOpenApi();

// PUT /forecasts/{date} updates an existing forecast's value
app.MapPut("/forecasts/{date}", (
    string date,
    UpdateForecastRequest request,
    ForecastStore store) =>
{
    if (!DateOnly.TryParse(date, out var parsedDate))
    {
        return Results.BadRequest(new ProblemDetails { Title = "Invalid date format", Detail = "Use YYYY-MM-DD" });
    }

    var index = store.Forecasts.FindIndex(f => f.Date == parsedDate);
    if (index < 0)
    {
        return Results.NotFound();
    }

    var updated = new Forecast(parsedDate, request.Value);
    store.Forecasts[index] = updated;
    return Results.Ok(updated);
})
.WithName("UpdateForecast")
.WithOpenApi();

app.MapGet("/hej", Results<Ok<string>, BadRequest<ProblemDetails>> () => Ok("Hej fra dit .NET API"))
.WithName("API-Track-API")
.WithOpenApi();

Console.Out.WriteLine("Swagger UI: http://localhost:5198/swagger");
app.Run();

// Needed for WebApplicationFactory<T> test discovery
public partial class Program { }

// Simple model
public record Forecast(DateOnly Date, decimal Value);
public record UpdateForecastRequest(decimal Value);

// In-memory store with seeded sample data
public class ForecastStore
{
    public List<Forecast> Forecasts { get; } = new();

    public ForecastStore()
    {
        // Seed 7 days (today + next 6) with deterministic demo values
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        for (var i = 0; i < 7; i++)
        {
            var date = today.AddDays(i);
            // Simple sinusoidal-ish pattern using deterministic math
            var value = 100m + (decimal)Math.Round(25 * Math.Sin(i / 3.0), 2) + i * 5;
            Forecasts.Add(new Forecast(date, value));
        }
    }
}
