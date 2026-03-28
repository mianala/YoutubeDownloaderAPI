using System.IO.Compression;
using System.Text;
using Gress;
using Microsoft.AspNetCore.WebUtilities;
using YoutubeDownloader.Api.Models;
using YoutubeDownloader.Core.Downloading;
using YoutubeDownloader.Core.Resolving;
using YoutubeExplode;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.ClosedCaptions;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloader.Api.Services;

public sealed class YoutubeDownloadApiService(DownloadProgressTracker progressTracker)
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
        string? container,
        string? quality,
        bool audioOnly = false,
        bool includeSrt = true,
        bool includeDescription = true,
        string? subtitleLanguage = null,
        string? progressId = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!string.IsNullOrWhiteSpace(progressId))
            progressTracker.Start(progressId, "Preparing download...");

        var selection = await SelectDownloadAsync(
            videoId,
            container,
            quality,
            audioOnly,
            cancellationToken
        );

        using var downloader = new VideoDownloader();
        var warnings = new List<string>();

        if (!includeSrt && !includeDescription)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{selection.Container.Name}");
            await downloader.DownloadVideoAsync(
                tempPath,
                selection.Video,
                selection.Option,
                includeSubtitles: false,
                progress: CreateProgressReporter(progressId, 0, 1, selection.FileName, "Downloading media..."),
                cancellationToken: cancellationToken
            );

            if (!string.IsNullOrWhiteSpace(progressId))
                progressTracker.Complete(progressId, selection.FileName, warnings);

            return new PreparedDownload(
                tempPath,
                selection.FileName,
                selection.ContentType,
                selection.Container.Name,
                selection.Option.IsAudioOnly,
                selection.Option.VideoQuality?.Label,
                false,
                false,
                false,
                warnings
            );
        }

        var workDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);

        var mediaPath = Path.Combine(workDir, selection.FileName);
        await downloader.DownloadVideoAsync(
            mediaPath,
            selection.Video,
            selection.Option,
            includeSubtitles: false,
            progress: CreateProgressReporter(progressId, 0, 0.85, selection.FileName, "Downloading media..."),
            cancellationToken: cancellationToken
        );

        var addedSrt = false;
        if (includeSrt)
        {
            var subtitlePath = Path.Combine(
                workDir,
                $"{Path.GetFileNameWithoutExtension(selection.FileName)}.srt"
            );

            try
            {
                ReportProgress(progressId, "downloading-subtitles", 0.9, "Downloading subtitles...", selection.FileName);
                await DownloadSubtitleAsync(
                    selection.Video.Id,
                    subtitlePath,
                    subtitleLanguage,
                    cancellationToken
                );

                addedSrt = true;
            }
            catch (RequestLimitExceededException)
            {
                const string warning = "Subtitles were skipped because YouTube rate-limited caption requests.";
                warnings.Add(warning);
                if (!string.IsNullOrWhiteSpace(progressId))
                    progressTracker.AddWarning(progressId, warning);
            }
            catch (KeyNotFoundException)
            {
                const string warning = "Subtitles were requested but no matching subtitle track was found.";
                warnings.Add(warning);
                if (!string.IsNullOrWhiteSpace(progressId))
                    progressTracker.AddWarning(progressId, warning);
            }
        }

        var addedDescription = false;
        if (includeDescription && selection.Video is Video { Description: { Length: > 0 } description })
        {
            var descriptionPath = Path.Combine(workDir, "description.txt");
            await File.WriteAllTextAsync(descriptionPath, description, Encoding.UTF8, cancellationToken);
            addedDescription = true;
        }

        ReportProgress(progressId, "packaging", 0.97, "Packaging download...", selection.FileName);
        var archiveName = $"{Path.GetFileNameWithoutExtension(selection.FileName)}.zip";
        var archivePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.zip");
        ZipFile.CreateFromDirectory(workDir, archivePath);
        Directory.Delete(workDir, recursive: true);

        if (!string.IsNullOrWhiteSpace(progressId))
            progressTracker.Complete(progressId, archiveName, warnings);

        return new PreparedDownload(
            archivePath,
            archiveName,
            "application/zip",
            selection.Container.Name,
            selection.Option.IsAudioOnly,
            selection.Option.VideoQuality?.Label,
            addedSrt,
            addedDescription,
            true,
            warnings
        );
    }

    public async Task<DownloadLinkResponse> GetDownloadLinkAsync(
        string videoId,
        string? container,
        string? quality,
        bool audioOnly = false,
        bool includeSrt = true,
        bool includeDescription = true,
        string? subtitleLanguage = null,
        string? progressId = null,
        CancellationToken cancellationToken = default
    )
    {
        var selection = await SelectDownloadAsync(
            videoId,
            container,
            quality,
            audioOnly,
            cancellationToken
        );

        var isArchive = includeSrt || includeDescription;
        var fileName = isArchive
            ? $"{Path.GetFileNameWithoutExtension(selection.FileName)}.zip"
            : selection.FileName;
        var contentType = isArchive ? "application/zip" : selection.ContentType;

        return new DownloadLinkResponse(
            BuildDownloadRelativeUrl(
                videoId,
                selection.Container.Name,
                selection.Option.VideoQuality?.Label ?? quality,
                audioOnly,
                includeSrt,
                includeDescription,
                subtitleLanguage,
                progressId
            ),
            fileName,
            contentType,
            selection.Container.Name,
            selection.Option.IsAudioOnly,
            selection.Option.VideoQuality?.Label,
            includeSrt,
            includeDescription,
            isArchive
        );
    }

    private async Task<SelectedDownload> SelectDownloadAsync(
        string videoId,
        string? container,
        string? quality,
        bool audioOnly,
        CancellationToken cancellationToken
    )
    {
        var parsedId = ParseVideoId(videoId);
        var targetContainer = ResolveRequestedContainer(container, audioOnly);

        using var downloader = new VideoDownloader();
        var options = await downloader.GetDownloadOptionsAsync(parsedId, cancellationToken: cancellationToken);

        var option = ResolveDownloadOption(options, targetContainer, quality, audioOnly);
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

    private static Container ResolveRequestedContainer(string? container, bool audioOnly) =>
        !string.IsNullOrWhiteSpace(container)
            ? ParseContainer(container)
            : audioOnly
                ? Container.Mp3
                : Container.Mp4;

    private static VideoDownloadOption? ResolveDownloadOption(
        IReadOnlyList<VideoDownloadOption> options,
        Container targetContainer,
        string? quality,
        bool audioOnly
    )
    {
        var matchingContainerOptions = options
            .Where(o => o.Container == targetContainer && (!audioOnly || o.IsAudioOnly))
            .ToArray();

        if (!string.IsNullOrWhiteSpace(quality))
        {
            var startsWithMatch = matchingContainerOptions.FirstOrDefault(o =>
                o.VideoQuality?.Label?.StartsWith(quality, StringComparison.OrdinalIgnoreCase) == true
            );

            return startsWithMatch ?? matchingContainerOptions.FirstOrDefault(o =>
                string.Equals(o.VideoQuality?.Label, quality, StringComparison.OrdinalIgnoreCase)
            );
        }

        return matchingContainerOptions
            .OrderByDescending(o => o.VideoQuality)
            .FirstOrDefault();
    }

    private static string BuildDownloadRelativeUrl(
        string videoId,
        string container,
        string? quality,
        bool audioOnly,
        bool includeSrt,
        bool includeDescription,
        string? subtitleLanguage,
        string? progressId
    )
    {
        var query = new Dictionary<string, string?> { ["container"] = container };
        if (!string.IsNullOrWhiteSpace(quality))
            query["quality"] = quality;
        if (audioOnly)
            query["audioOnly"] = "true";
        if (includeSrt)
            query["includeSrt"] = "true";
        if (includeDescription)
            query["includeDescription"] = "true";
        if (!string.IsNullOrWhiteSpace(subtitleLanguage))
            query["subtitleLanguage"] = subtitleLanguage;
        if (!string.IsNullOrWhiteSpace(progressId))
            query["progressId"] = progressId;

        var path = audioOnly ? $"/api/audio/{videoId}" : $"/api/videos/{videoId}/download";
        return QueryHelpers.AddQueryString(path, query);
    }

    private IProgress<Percentage>? CreateProgressReporter(
        string? progressId,
        double start,
        double end,
        string fileName,
        string message
    )
    {
        if (string.IsNullOrWhiteSpace(progressId))
            return null;

        return new Progress<Percentage>(percentage =>
        {
            var fraction = percentage.Fraction;
            var progress = start + ((end - start) * fraction);
            progressTracker.Report(progressId, "downloading-media", progress, message, fileName);
        });
    }

    private void ReportProgress(
        string? progressId,
        string status,
        double progress,
        string message,
        string? fileName = null
    )
    {
        if (string.IsNullOrWhiteSpace(progressId))
            return;

        progressTracker.Report(progressId, status, progress, message, fileName);
    }

    private static async Task DownloadSubtitleAsync(
        VideoId videoId,
        string subtitlePath,
        string? subtitleLanguage,
        CancellationToken cancellationToken
    )
    {
        using var youtube = new YoutubeClient();
        var manifest = await youtube.Videos.ClosedCaptions.GetManifestAsync(videoId, cancellationToken);
        var trackInfo = SelectSubtitleTrack(manifest, subtitleLanguage);
        if (trackInfo is null)
            throw new KeyNotFoundException("No subtitle track found.");

        var track = await youtube.Videos.ClosedCaptions.GetAsync(trackInfo, cancellationToken);

        await using var stream = File.Create(subtitlePath);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));

        for (var i = 0; i < track.Captions.Count; i++)
        {
            var caption = track.Captions[i];
            await writer.WriteLineAsync((i + 1).ToString());
            await writer.WriteLineAsync(
                $"{FormatSrtTime(caption.Offset)} --> {FormatSrtTime(caption.Offset + caption.Duration)}"
            );
            await writer.WriteLineAsync(caption.Text);
            await writer.WriteLineAsync();
        }
    }

    private static ClosedCaptionTrackInfo? SelectSubtitleTrack(
        ClosedCaptionManifest manifest,
        string? subtitleLanguage
    )
    {
        if (!string.IsNullOrWhiteSpace(subtitleLanguage))
        {
            var exact = manifest.TryGetByLanguage(subtitleLanguage);
            if (exact is not null)
                return exact;

            return manifest.Tracks.FirstOrDefault(t =>
                string.Equals(t.Language.Code, subtitleLanguage, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t.Language.Name, subtitleLanguage, StringComparison.OrdinalIgnoreCase)
            );
        }

        return manifest.Tracks
            .OrderBy(t => t.IsAutoGenerated)
            .ThenBy(t => t.Language.Code, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static string FormatSrtTime(TimeSpan time) =>
        $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00},{time.Milliseconds:000}";

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

public sealed record PreparedDownload(
    string TempPath,
    string FileName,
    string ContentType,
    string Container,
    bool IsAudioOnly,
    string? VideoQuality,
    bool IncludesSrt,
    bool IncludesDescription,
    bool IsArchive
    ,
    IReadOnlyList<string> Warnings
);
