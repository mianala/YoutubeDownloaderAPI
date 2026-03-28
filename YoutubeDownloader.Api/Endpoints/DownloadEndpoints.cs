using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YoutubeDownloader.Api.Services;

namespace YoutubeDownloader.Api.Endpoints;

public static class DownloadEndpoints
{
    public static void MapDownloadEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/videos/{videoId}/download", async (
            string videoId,
            string container,
            string? quality,
            YoutubeDownloadApiService service,
            CancellationToken ct) =>
        {
            try
            {
                var preparedDownload = await service.PrepareDownloadAsync(videoId, container, quality, ct);

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
        .WithDescription("Streams the selected download option back to the caller as a file attachment.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);
    }
}
