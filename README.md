# Moodtune

A mood-based song recommender built with ASP.NET Core 8. Describe how you feel, choose a genre and energy level, and Moodtune finds a small playlist from its song catalog. Each song links to a Spotify search.

## Run the app

Install the .NET 8 SDK, then run these commands from the project folder:

```powershell
dotnet run
```

Open the local URL printed by ASP.NET Core.

## Run local AI recommendations

Moodtune uses Ollama to run recommendations with a local language model; no paid API key is required. Install Ollama from [ollama.com/download](https://ollama.com/download), then open PowerShell and download the default model:

```powershell
ollama pull qwen2.5:3b
```

Keep Ollama running in the background. In another PowerShell window, start Moodtune:

```powershell
dotnet run
```

The default local model is `qwen2.5:3b` (about 2 GB to download); set `OLLAMA_MODEL` in the server environment to use another model already installed in Ollama. Moodtune sends prompts to the local Ollama service at `http://localhost:11434`, not to a paid cloud API. If Ollama is not running, the model is missing, or a local AI request fails, the app displays an error rather than silently switching to non-AI recommendations.

## API

- `GET /api/health` — service health check.
- `POST /api/recommendations` — request songs with a JSON body such as `{"mood":"a little restless, but hopeful","genre":"Indie","energy":6,"count":5}`.
