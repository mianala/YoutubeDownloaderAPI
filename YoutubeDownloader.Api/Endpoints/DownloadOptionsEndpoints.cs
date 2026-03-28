using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YoutubeDownloader.Api.Models;
using YoutubeDownloader.Core.Downloading;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.Api.Endpoints;

public static class DownloadOptionsEndpoints
{
    public static void MapDownloadOptionsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/videos/{videoId}/download-options", async (string videoId, CancellationToken ct) =>
        {
            if (VideoId.TryParse(videoId) is not { } parsedId)
                return Results.BadRequest(new { error = "Invalid video ID." });

            using var downloader = new VideoDownloader();
            var options = await downloader.GetDownloadOptionsAsync(parsedId, cancellationToken: ct);

            var response = options.Select(o => new DownloadOptionInfo(
                o.Container.Name,
                o.IsAudioOnly,
                o.VideoQuality?.Label
            )).ToList();

            return Results.Ok(response);
        });
    }
}
