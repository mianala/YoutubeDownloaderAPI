using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YoutubeDownloader.Api.Models;
using YoutubeDownloader.Api.Services;

namespace YoutubeDownloader.Api.Endpoints;

public static class DownloadOptionsEndpoints
{
    public static void MapDownloadOptionsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/videos/{videoId}/download-options", async (
                string videoId,
                YoutubeDownloadApiService service,
                CancellationToken ct) =>
            {
                try
                {
                    var response = await service.GetDownloadOptionsAsync(videoId, ct);
                    return Results.Ok(response);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("GetDownloadOptions")
            .WithTags("Downloads")
            .WithSummary("List available download options for a video.")
            .WithDescription("Returns the containers and video qualities available for the requested YouTube video.")
            .Produces<IReadOnlyList<DownloadOptionInfo>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
    }
}
