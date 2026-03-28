using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using YoutubeDownloader.Api.Models;
using YoutubeDownloader.Api.Services;

namespace YoutubeDownloader.Api.Endpoints;

public static class DownloadEndpoints
{
    public static void MapDownloadEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/audio/{videoId}", async (
            string videoId,
            [AsParameters] AudioDownloadRequest request,
            YoutubeDownloadApiService service,
            CancellationToken ct) =>
        {
            try
            {
                var preparedDownload = await service.PrepareDownloadAsync(
                    videoId,
                    request.Container,
                    request.Quality,
                    audioOnly: true,
                    includeSrt: request.IncludeSrt ?? true,
                    includeDescription: request.IncludeDescription ?? true,
                    subtitleLanguage: request.SubtitleLanguage,
                    cancellationToken: ct
                );

                var stream = new FileStream(
                    preparedDownload.TempPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.None,
                    bufferSize: 64 * 1024,
                    FileOptions.DeleteOnClose | FileOptions.SequentialScan
                );

                return Results.File(stream, preparedDownload.ContentType, preparedDownload.FileName);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .WithName("DownloadAudio")
        .WithTags("Downloads")
        .WithSummary("Download audio from a YouTube video.")
        .WithDescription("Downloads an audio-only version of the requested YouTube video. When includeSrt=true or includeDescription=true, the API returns a zip archive containing the audio file plus the requested sidecar files.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/videos/{videoId}/download", async (
            string videoId,
            [AsParameters] DownloadRequest request,
            YoutubeDownloadApiService service,
            CancellationToken ct) =>
        {
            try
            {
                var preparedDownload = await service.PrepareDownloadAsync(
                    videoId,
                    request.Container,
                    request.Quality,
                    request.AudioOnly,
                    request.IncludeSrt ?? true,
                    request.IncludeDescription ?? true,
                    request.SubtitleLanguage,
                    ct
                );

                // Stream the file back and delete on close.
                var stream = new FileStream(
                    preparedDownload.TempPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.None,
                    bufferSize: 64 * 1024,
                    FileOptions.DeleteOnClose | FileOptions.SequentialScan
                );

                return Results.File(stream, preparedDownload.ContentType, preparedDownload.FileName);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .WithName("DownloadVideo")
        .WithTags("Downloads")
        .WithSummary("Download a YouTube video or audio file.")
        .WithDescription("Downloads a video or audio file from YouTube. Use audioOnly=true for audio extraction, or prefer /api/audio/{videoId} for the dedicated audio route. When includeSrt=true or includeDescription=true, the API returns a zip archive containing the media file plus the requested sidecar files.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);
    }
}
