using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using YoutubeDownloader.Api.Models;
using YoutubeDownloader.Api.Services;
using YoutubeExplode.Exceptions;

namespace YoutubeDownloader.Api.Endpoints;

public static class DownloadEndpoints
{
    public static void MapDownloadEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/downloads/progress/{progressId}", (
            string progressId,
            DownloadProgressTracker progressTracker) =>
        {
            var progress = progressTracker.Get(progressId);
            return progress is null
                ? Results.NotFound(new { error = "Progress entry not found." })
                : Results.Ok(progress);
        })
        .WithName("GetDownloadProgress")
        .WithTags("Downloads")
        .WithSummary("Get the progress of a download preparation request.")
        .WithDescription("Poll this endpoint with the same progressId you passed to a download route to track media download, subtitle fetch, packaging, completion, warnings, or failures.")
        .Produces<DownloadProgressResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/audio/{videoId}", async (
            string videoId,
            [AsParameters] AudioDownloadRequest request,
            HttpContext httpContext,
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
                    progressId: request.ProgressId,
                    cancellationToken: ct
                );

                if (preparedDownload.Warnings.Count > 0)
                    httpContext.Response.Headers.Append("X-YoutubeDownloader-Warnings", string.Join(" | ", preparedDownload.Warnings));

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
            catch (RequestLimitExceededException)
            {
                return Results.Problem(
                    title: "YouTube rate limit exceeded",
                    detail: "YouTube temporarily rejected the request due to rate limiting. Try again later or configure authenticated cookies.",
                    statusCode: StatusCodes.Status429TooManyRequests
                );
            }
        })
        .WithName("DownloadAudio")
        .WithTags("Downloads")
        .WithSummary("Download audio from a YouTube video.")
        .WithDescription("Downloads an audio-only version of the requested YouTube video. Pass a client-generated progressId and poll /api/downloads/progress/{progressId} while the file is being prepared. When includeSrt=true or includeDescription=true, the API returns a zip archive containing the audio file plus the requested sidecar files.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);

        app.MapGet("/api/videos/{videoId}/download", async (
            string videoId,
            [AsParameters] DownloadRequest request,
            HttpContext httpContext,
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
                    request.ProgressId,
                    ct
                );

                if (preparedDownload.Warnings.Count > 0)
                    httpContext.Response.Headers.Append("X-YoutubeDownloader-Warnings", string.Join(" | ", preparedDownload.Warnings));

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
            catch (RequestLimitExceededException)
            {
                return Results.Problem(
                    title: "YouTube rate limit exceeded",
                    detail: "YouTube temporarily rejected the request due to rate limiting. Try again later or configure authenticated cookies.",
                    statusCode: StatusCodes.Status429TooManyRequests
                );
            }
        })
        .WithName("DownloadVideo")
        .WithTags("Downloads")
        .WithSummary("Download a YouTube video or audio file.")
        .WithDescription("Downloads a video or audio file from YouTube. Use audioOnly=true for audio extraction, or prefer /api/audio/{videoId} for the dedicated audio route. Pass a client-generated progressId and poll /api/downloads/progress/{progressId} while the file is being prepared. When includeSrt=true or includeDescription=true, the API returns a zip archive containing the media file plus the requested sidecar files.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }
}
