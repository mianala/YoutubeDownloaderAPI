# YoutubeDownloaderAPI

This repository is a fork-based wrapper around [Tyrrrz/YoutubeDownloader](https://github.com/Tyrrrz/YoutubeDownloader).

The upstream project provides the core YouTube download logic and desktop app. This fork adds a lightweight ASP.NET Core API layer on top of that functionality so it can be used over HTTP and by MCP-compatible clients.

## What This Fork Adds

- A minimal HTTP API in [YoutubeDownloader.Api](./YoutubeDownloader.Api)
- OpenAPI document generation
- Swagger UI for interactive testing in the browser
- MCP endpoint for MCP-compatible tools and agents

## API Endpoints

- REST API: `/api/...`
- Dedicated audio download route: `/api/audio/{videoId}`
- OpenAPI JSON: `/openapi/v1.json`
- Swagger UI: `/swagger`
- MCP: `/mcp`
- Download progress polling: `/api/downloads/progress/{progressId}`

`/` and `/openapi` redirect to Swagger UI for easier local testing.

## Run The API

Start the API server from the repository root with:

```bash
dotnet run --project YoutubeDownloader.Api
```

Then open:

- Swagger UI: `http://localhost:5000/swagger`
- OpenAPI JSON: `http://localhost:5000/openapi/v1.json`

## What The API Exposes

- Resolve a YouTube URL or search query into videos
- List available download options for a video
- Download a video over HTTP
- Download audio through `/api/audio/{videoId}`
- Optionally package downloads with an `.srt` subtitle file and `description.txt`
- Track preparation progress by passing `progressId` and polling `/api/downloads/progress/{progressId}`
- Access the same core capabilities through MCP tools

## Upstream Project

The original project lives here:

- [Tyrrrz/YoutubeDownloader](https://github.com/Tyrrrz/YoutubeDownloader)

If you want the full desktop application, original screenshots, release notes, and upstream project details, use that repository as the primary reference.

## Notes

- This fork reuses the upstream download engine rather than reimplementing it.
- The API and MCP layers are the main additions in this repository.
