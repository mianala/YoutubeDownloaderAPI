using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YoutubeDownloader.Api.Models;
using YoutubeDownloader.Core.Resolving;

namespace YoutubeDownloader.Api.Endpoints;

public static class ResolveEndpoints
{
    public static void MapResolveEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/resolve", async (string query, CancellationToken ct) =>
        {
            using var resolver = new QueryResolver();
            var result = await resolver.ResolveAsync(query, ct);

            var response = new ResolveResponse(
                result.Kind.ToString(),
                result.Title,
                result.Videos.Select(v => new VideoInfo(
                    v.Id.Value,
                    v.Title,
                    v.Author.ChannelTitle,
                    v.Duration,
                    v.Thumbnails.MaxBy(t => t.Resolution.Area)?.Url
                )).ToList()
            );

            return Results.Ok(response);
        });
    }
}
