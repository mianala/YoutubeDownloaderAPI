using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YoutubeDownloader.Api.Models;
using YoutubeDownloader.Api.Services;

namespace YoutubeDownloader.Api.Endpoints;

public static class ResolveEndpoints
{
    public static void MapResolveEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/resolve", async (
                string query,
                YoutubeDownloadApiService service,
                CancellationToken ct) =>
            {
                var response = await service.ResolveAsync(query, ct);
                return Results.Ok(response);
            })
            .WithName("ResolveQuery")
            .WithTags("Resolve")
            .WithSummary("Resolve a YouTube URL or search query.")
            .WithDescription("Returns the resolved query type together with the matching videos.")
            .Produces<ResolveResponse>(StatusCodes.Status200OK);
    }
}
