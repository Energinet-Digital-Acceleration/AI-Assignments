using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.TypedResults;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

app.MapGet("/hej", Results<Ok<string>, BadRequest<ProblemDetails>> () => Ok("Hej fra dit .NET API"))
.WithName("API-Track-API")
.WithOpenApi();

Console.Out.WriteLine("Swagger UI: http://localhost:5198/swagger");
app.Run();
