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

    [McpServerTool, Description("List the available download containers and qualities for a YouTube video. Audio-only options are marked with IsAudioOnly.")]
    public static Task<IReadOnlyList<DownloadOptionInfo>> GetDownloadOptions(
        YoutubeDownloadApiService service,
        [Description("The YouTube video ID to inspect.")] string videoId,
        CancellationToken cancellationToken
    ) => service.GetDownloadOptionsAsync(videoId, cancellationToken);

    [McpServerTool, Description("Build a direct HTTP download URL for a chosen video or audio download. When subtitles or the description are requested, the HTTP response is a zip archive containing the media file plus sidecar files.")]
    public static Task<DownloadLinkResponse> GetDownloadLink(
        YoutubeDownloadApiService service,
        [Description("The YouTube video ID to download.")] string videoId,
        [Description("The desired output container such as mp4, webm, mp3, or ogg. Omit it to use the default for the selected mode.")]
        string? container,
        [Description("Optional quality label such as 1080p60, 720p, or 360p. If omitted, the highest quality for the chosen container is used.")]
        string? quality,
        [Description("When true, selects an audio-only download option. If no container is provided, mp3 is used by default.")]
        bool audioOnly,
        [Description("When true, the download is packaged as a zip file with an .srt subtitle file when captions are available. Defaults to true in the HTTP API.")]
        bool includeSrt,
        [Description("When true, the download is packaged as a zip file with a description.txt file when the video has a description. Defaults to true in the HTTP API.")]
        bool includeDescription,
        [Description("Optional subtitle language code or language name to use for the generated .srt file, for example 'en'.")]
        string? subtitleLanguage,
        [Description("Optional client-generated ID used to poll the HTTP progress endpoint while the file is being prepared.")]
        string? progressId,
        CancellationToken cancellationToken
    ) => service.GetDownloadLinkAsync(
        videoId,
        container,
        quality,
        audioOnly,
        includeSrt,
        includeDescription,
        subtitleLanguage,
        progressId,
        cancellationToken
    );
}
