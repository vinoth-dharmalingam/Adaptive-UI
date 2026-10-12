using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MoodTune;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<SongCatalog>();
builder.Services.AddSingleton<RecommendationService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/recommendations", async (
    RecommendationRequest request,
    RecommendationService recommendations,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Mood) || request.Mood.Length > 240)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["mood"] = ["Tell us how you're feeling in 1–240 characters."]
        });
    }

    if (request.Count is < 1 or > 8)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["count"] = ["Choose between 1 and 8 songs."]
        });
    }

    try
    {
        var result = await recommendations.GetAsync(request, cancellationToken);
        return Results.Ok(result);
    }
    catch (HttpRequestException exception)
    {
        app.Logger.LogError(exception, "The local AI recommendation request failed.");
        return Results.Problem(
            "The local AI service is unavailable. Make sure Ollama is running and the selected model is installed, then try again.",
            statusCode: StatusCodes.Status502BadGateway);
    }
    catch (JsonException exception)
    {
        app.Logger.LogError(exception, "The local AI recommendation response could not be read.");
        return Results.Problem(
            "The local AI returned an unreadable response. Please try again.",
            statusCode: StatusCodes.Status502BadGateway);
    }
});

app.Run();
