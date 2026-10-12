using System.Text;
using System.Text.Json;

namespace MoodTune;

public sealed record RecommendationRequest(string Mood, string Genre = "Any", int Energy = 5, int Count = 5);

public sealed record SongRecommendation(Song Song, string Reason);

public sealed record RecommendationResult(
    string Source,
    string Intro,
    IReadOnlyList<SongRecommendation> Recommendations);

public sealed class RecommendationService(
    SongCatalog catalog,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public Task<RecommendationResult> GetAsync(
        RecommendationRequest request,
        CancellationToken cancellationToken)
    {
        return GetAiRecommendationsAsync(request, cancellationToken);
    }

    private async Task<RecommendationResult> GetAiRecommendationsAsync(
        RecommendationRequest request,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(3);
        using var message = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/chat");

        var songsJson = JsonSerializer.Serialize(catalog.Songs, JsonOptions);
        message.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                model = configuration["OLLAMA_MODEL"] ?? "qwen2.5:3b",
                stream = false,
                format = "json",
                options = new { temperature = 0.7 },
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "You are a warm, thoughtful music curator. Recommend only songs from the supplied catalog; never invent songs or song IDs. Match the person's stated mood, requested genre, and energy level. Return only valid JSON with an intro string and a recommendations array of objects, each with id and a short, personal reason. Do not diagnose or make assumptions about the person."
                    },
                    new
                    {
                        role = "user",
                        content = $"Mood: {request.Mood}\nPreferred genre: {request.Genre}\nDesired energy (1-10): {Math.Clamp(request.Energy, 1, 10)}\nSelect up to {Math.Clamp(request.Count, 1, 8)} different songs.\nCatalog: {songsJson}"
                    }
                }
            }, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await client.SendAsync(message, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Ollama returned HTTP {(int)response.StatusCode}: {responseBody}",
                null,
                response.StatusCode);
        }

        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        if (!root.TryGetProperty("message", out var messageElement)
            || !messageElement.TryGetProperty("content", out var contentElement)
            || contentElement.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("The Ollama response did not contain a valid chat message.");
        }

        var content = contentElement.GetString();
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new JsonException("The Ollama response did not contain recommendation text.");
        }

        var aiResult = JsonSerializer.Deserialize<AiRecommendationResponse>(content, JsonOptions)
            ?? throw new JsonException("The AI response was empty.");
        if (aiResult.Recommendations is null)
        {
            throw new JsonException("The AI response did not include a recommendations list.");
        }

        var songsById = catalog.Songs.ToDictionary(song => song.Id, StringComparer.Ordinal);
        var recommendations = aiResult.Recommendations
            .Where(item => item.Id is { Length: > 0 } id && songsById.ContainsKey(id))
            .DistinctBy(item => item.Id)
            .Take(Math.Clamp(request.Count, 1, 8))
            .Select(item => new SongRecommendation(
                songsById[item.Id!],
                string.IsNullOrWhiteSpace(item.Reason) ? "Picked to fit the mood you shared." : item.Reason))
            .ToArray();

        if (recommendations.Length == 0)
        {
            throw new JsonException("The AI response did not select any songs from the catalog.");
        }

        return new RecommendationResult(
            "local-ai",
            string.IsNullOrWhiteSpace(aiResult.Intro)
                ? "A little soundtrack for exactly where you are."
                : aiResult.Intro,
            recommendations);
    }

    private sealed record AiRecommendationResponse(
        string? Intro,
        IReadOnlyList<AiSongRecommendation>? Recommendations);

    private sealed record AiSongRecommendation(string? Id, string? Reason);
}
