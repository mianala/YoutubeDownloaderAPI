using Microsoft.AspNetCore.WebUtilities;
using YoutubeDownloader.Api.Models;
using YoutubeDownloader.Core.Downloading;
using YoutubeDownloader.Core.Resolving;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloader.Api.Services;

public sealed class YoutubeDownloadApiService
{
    public async Task<ResolveResponse> ResolveAsync(string query, CancellationToken cancellationToken = default)
    {
        using var resolver = new QueryResolver();
        var result = await resolver.ResolveAsync(query, cancellationToken);

        return new ResolveResponse(
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
    }

    public async Task<IReadOnlyList<DownloadOptionInfo>> GetDownloadOptionsAsync(
        string videoId,
        CancellationToken cancellationToken = default
    )
    {
        var parsedId = ParseVideoId(videoId);

        using var downloader = new VideoDownloader();
        var options = await downloader.GetDownloadOptionsAsync(parsedId, cancellationToken: cancellationToken);

        return options.Select(o => new DownloadOptionInfo(
            o.Container.Name,
            o.IsAudioOnly,
            o.VideoQuality?.Label
        )).ToList();
    }

    public async Task<PreparedDownload> PrepareDownloadAsync(
        string videoId,
        string container,
        string? quality,
        CancellationToken cancellationToken = default
    )
    {
        var selection = await SelectDownloadAsync(videoId, container, quality, cancellationToken);

        using var downloader = new VideoDownloader();

        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{selection.Container.Name}");
        await downloader.DownloadVideoAsync(
            tempPath,
            selection.Video,
            selection.Option,
            includeSubtitles: false,
            cancellationToken: cancellationToken
        );

        return new PreparedDownload(tempPath, selection.FileName, selection.ContentType);
    }

    public async Task<DownloadLinkResponse> GetDownloadLinkAsync(
        string videoId,
        string container,
        string? quality,
        CancellationToken cancellationToken = default
    )
    {
        var selection = await SelectDownloadAsync(videoId, container, quality, cancellationToken);

        return new DownloadLinkResponse(
            BuildDownloadRelativeUrl(videoId, selection.Container.Name, selection.Option.VideoQuality?.Label ?? quality),
            selection.FileName,
            selection.ContentType,
            selection.Container.Name,
            selection.Option.IsAudioOnly,
            selection.Option.VideoQuality?.Label
        );
    }

    private async Task<SelectedDownload> SelectDownloadAsync(
        string videoId,
        string container,
        string? quality,
        CancellationToken cancellationToken
    )
    {
        var parsedId = ParseVideoId(videoId);
        var targetContainer = ParseContainer(container);

        using var downloader = new VideoDownloader();
        var options = await downloader.GetDownloadOptionsAsync(parsedId, cancellationToken: cancellationToken);

        var option = ResolveDownloadOption(options, targetContainer, quality);
        if (option is null)
            throw new KeyNotFoundException("No matching download option found.");

        using var youtube = new YoutubeClient();
        var video = await youtube.Videos.GetAsync(parsedId, cancellationToken);

        var safeTitle = string.Join("_", video.Title.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"{safeTitle}.{targetContainer.Name}";
        var contentType = GetContentType(targetContainer.Name, option.IsAudioOnly);

        return new SelectedDownload(video, targetContainer, option, fileName, contentType);
    }

    private static VideoId ParseVideoId(string videoId) =>
        VideoId.TryParse(videoId) ?? throw new ArgumentException("Invalid video ID.", nameof(videoId));

    private static Container ParseContainer(string container)
    {
        try
        {
            return new Container(container);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            throw new ArgumentException("Invalid container.", nameof(container), ex);
        }
    }

    private static VideoDownloadOption? ResolveDownloadOption(
        IReadOnlyList<VideoDownloadOption> options,
        Container targetContainer,
        string? quality
    )
    {
        if (!string.IsNullOrWhiteSpace(quality))
        {
            var startsWithMatch = options.FirstOrDefault(o =>
                o.Container == targetContainer &&
                o.VideoQuality?.Label?.StartsWith(quality, StringComparison.OrdinalIgnoreCase) == true
            );

            return startsWithMatch ?? options.FirstOrDefault(o =>
                o.Container == targetContainer &&
                string.Equals(o.VideoQuality?.Label, quality, StringComparison.OrdinalIgnoreCase)
            );
        }

        return options
            .Where(o => o.Container == targetContainer)
            .OrderByDescending(o => o.VideoQuality)
            .FirstOrDefault();
    }

    private static string BuildDownloadRelativeUrl(string videoId, string container, string? quality)
    {
        var query = new Dictionary<string, string?> { ["container"] = container };
        if (!string.IsNullOrWhiteSpace(quality))
            query["quality"] = quality;

        return QueryHelpers.AddQueryString($"/api/videos/{videoId}/download", query);
    }

    private static string GetContentType(string container, bool isAudioOnly) =>
        container.ToLowerInvariant() switch
        {
            "mp4" when isAudioOnly => "audio/mp4",
            "mp4" => "video/mp4",
            "webm" when isAudioOnly => "audio/webm",
            "webm" => "video/webm",
            "mp3" => "audio/mpeg",
            "ogg" => "audio/ogg",
            _ => "application/octet-stream"
        };

    private sealed record SelectedDownload(
        YoutubeExplode.Videos.Video Video,
        Container Container,
        VideoDownloadOption Option,
        string FileName,
        string ContentType
    );
}

public sealed record PreparedDownload(string TempPath, string FileName, string ContentType);
