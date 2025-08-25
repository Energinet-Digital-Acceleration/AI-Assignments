using System.Diagnostics;
using Energinet.Forecast.Api;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.TypedResults;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IForecastService, ForecastService>();

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

app.MapGet("/power/forecast", Results<Ok<ForecastResponse>, BadRequest<ProblemDetails>> (
    [FromQuery] string area, [FromQuery] int hours) =>
{
    var validAreas = new[] { "DK1", "DK2" };
    var upperArea = area.ToUpperInvariant();
    if (!validAreas.Contains(upperArea))
        return BadRequest(new ProblemDetails { Title = "Invalid area", Detail = "area must be DK1 or DK2" });
    if (hours is < 1 or > 48)
        return BadRequest(new ProblemDetails { Title = "Invalid hours", Detail = "hours must be between 1 and 48" });

    var svc = app.Services.GetRequiredService<IForecastService>();
    if (area == null) throw new InvalidOperationException();

    var points = svc.Generate(upperArea, hours);
    return Ok(new ForecastResponse(upperArea, points));
})
.WithName("GetForecast")
.WithOpenApi();

Console.Out.WriteLine("Swagger UI: http://localhost:5198/swagger");
app.Run();
