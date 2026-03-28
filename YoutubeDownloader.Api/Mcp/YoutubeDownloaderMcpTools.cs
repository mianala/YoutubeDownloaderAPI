using ModelContextProtocol.Server;
using System.ComponentModel;
using YoutubeDownloader.Api.Models;
using YoutubeDownloader.Api.Services;

namespace YoutubeDownloader.Api.Mcp;

[McpServerToolType]
public sealed class YoutubeDownloaderMcpTools
{
    [McpServerTool, Description("Resolve a YouTube URL, playlist, channel, or search query into matching videos.")]
    public static Task<ResolveResponse> ResolveQuery(
        YoutubeDownloadApiService service,
        [Description("The YouTube URL or search query to resolve.")] string query,
        CancellationToken cancellationToken
    ) => service.ResolveAsync(query, cancellationToken);

    [McpServerTool, Description("List the available download containers and qualities for a YouTube video.")]
    public static Task<IReadOnlyList<DownloadOptionInfo>> GetDownloadOptions(
        YoutubeDownloadApiService service,
        [Description("The YouTube video ID to inspect.")] string videoId,
        CancellationToken cancellationToken
    ) => service.GetDownloadOptionsAsync(videoId, cancellationToken);

    [McpServerTool, Description("Build a direct HTTP download URL for a chosen video container and optional quality.")]
    public static Task<DownloadLinkResponse> GetDownloadLink(
        YoutubeDownloadApiService service,
        [Description("The YouTube video ID to download.")] string videoId,
        [Description("The desired output container such as mp4, webm, mp3, or ogg.")] string container,
        [Description("Optional quality label such as 1080p60, 720p, or 360p. If omitted, the highest quality for the chosen container is used.")]
        string? quality,
        CancellationToken cancellationToken
    ) => service.GetDownloadLinkAsync(videoId, container, quality, cancellationToken);
}
