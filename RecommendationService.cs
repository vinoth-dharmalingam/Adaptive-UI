using System.Net.Http.Headers;
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
    private static readonly IReadOnlyDictionary<string, string[]> MoodKeywords =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["joyful"] = ["joyful", "happy", "joy", "lift", "excited", "cheerful", "upbeat"],
            ["blue"] = ["blue", "sad", "down", "lonely", "heartbroken", "low"],
            ["anxious"] = ["anxious", "anxiety", "stressed", "overwhelmed", "nervous", "tense"],
            ["focused"] = ["focused", "focus", "productive", "studying", "work"],
            ["restless"] = ["restless", "energized", "energy", "wired", "bored"],
            ["romantic"] = ["romantic", "love", "in love", "date"],
            ["hopeful"] = ["hopeful", "hope", "optimistic", "inspired", "lift", "fresh start", "encouraged"],
            ["calm"] = ["calm", "peaceful", "relaxed", "unwind", "gentle"],
            ["reflective"] = ["reflective", "thoughtful", "nostalgic", "thinking"],
            ["confident"] = ["confident", "powerful", "bold", "motivated", "strong"]
        };

    public Task<RecommendationResult> GetAsync(
        RecommendationRequest request,
        CancellationToken cancellationToken)
    {
        var apiKey = configuration["OPENAI_API_KEY"];
        return string.IsNullOrWhiteSpace(apiKey)
            ? Task.FromResult(GetCuratedRecommendations(request))
            : GetAiRecommendationsAsync(request, apiKey, cancellationToken);
    }

    private RecommendationResult GetCuratedRecommendations(RecommendationRequest request)
    {
        var mood = request.Mood.ToLowerInvariant();
        var matchingMoods = MoodKeywords
            .Where(item => item.Value.Any(keyword => mood.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .Select(item => item.Key)
            .ToArray();

        var ranked = catalog.Songs
            .Select(song => new
            {
                Song = song,
                MoodScore = matchingMoods.Length == 0
                    ? 0
                    : song.Moods.Count(tag => matchingMoods.Contains(tag, StringComparer.OrdinalIgnoreCase)),
                EnergyScore = Math.Abs(song.Energy - Math.Clamp(request.Energy, 1, 10)),
                GenreScore = string.Equals(request.Genre, "Any", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(song.Genre, request.Genre, StringComparison.OrdinalIgnoreCase)
                    ? 0
                    : 1
            })
            .OrderByDescending(item => item.MoodScore)
            .ThenBy(item => item.GenreScore)
            .ThenBy(item => item.EnergyScore)
            .ThenBy(item => item.Song.Title, StringComparer.Ordinal)
            .Take(Math.Clamp(request.Count, 1, 8))
            .Select(item => new SongRecommendation(
                item.Song,
                $"A {item.Song.Energy}/10 energy {item.Song.Genre.ToLowerInvariant()} pick for a {item.Song.Moods[0]} kind of moment."))
            .ToArray();

        return new RecommendationResult(
            "curated",
            "A handpicked starting point, tuned to your mood and energy.",
            ranked);
    }

    private async Task<RecommendationResult> GetAiRecommendationsAsync(
        RecommendationRequest request,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        using var message = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var songsJson = JsonSerializer.Serialize(catalog.Songs, JsonOptions);
        message.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                model = configuration["OPENAI_MODEL"] ?? "gpt-4o-mini",
                temperature = 0.7,
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = "You are a warm, thoughtful music curator. Recommend only songs from the supplied catalog; never invent songs or song IDs. Match the person's stated mood, requested genre, and energy level. Return valid JSON with an intro string and a recommendations array of objects, each with id and a short, personal reason. Do not diagnose or make assumptions about the person."
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
                $"OpenAI returned HTTP {(int)response.StatusCode}: {responseBody}",
                null,
                response.StatusCode);
        }

        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        if (!root.TryGetProperty("choices", out var choices)
            || choices.ValueKind != JsonValueKind.Array
            || choices.GetArrayLength() == 0
            || !choices[0].TryGetProperty("message", out var messageContent)
            || !messageContent.TryGetProperty("content", out var contentElement)
            || contentElement.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("The AI response did not contain a valid chat completion.");
        }

        var content = contentElement.GetString();
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new JsonException("The AI response did not contain recommendation content.");
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
            "ai",
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
