# Moodtune

A mood-based song recommender built with ASP.NET Core 8. Describe how you feel, choose a genre and energy level, and Moodtune finds a small playlist from its song catalog. Each song links to a Spotify search.

## Run the app

Install the .NET 8 SDK, then run these commands from the project folder:

```powershell
dotnet run
```

Open the local URL printed by ASP.NET Core.

## Enable AI recommendations

Without an API key, the app ranks its built-in song catalog using the selected mood, genre, and energy level. To enable AI-powered selections from that same catalog, set your OpenAI API key in the server's environment before starting the app:

```powershell
$env:OPENAI_API_KEY = "your-api-key"
dotnet run
```

The app sends requests from the server, so the key is never exposed to the browser. The default model is `gpt-4o-mini`; set `OPENAI_MODEL` in the server environment to use a different compatible model. If a configured AI request fails, the app reports an error instead of silently switching modes.

## API

- `GET /api/health` — service health check.
- `POST /api/recommendations` — request songs with a JSON body such as `{"mood":"a little restless, but hopeful","genre":"Indie","energy":6,"count":5}`.
