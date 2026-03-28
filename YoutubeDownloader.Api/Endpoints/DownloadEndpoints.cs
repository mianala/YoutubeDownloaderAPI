using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YoutubeDownloader.Core.Downloading;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloader.Api.Endpoints;

public static class DownloadEndpoints
{
    public static void MapDownloadEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/videos/{videoId}/download", async (
            string videoId,
            string container,
            string? quality,
            CancellationToken ct) =>
        {
            if (VideoId.TryParse(videoId) is not { } parsedId)
                return Results.BadRequest(new { error = "Invalid video ID." });

            using var downloader = new VideoDownloader();
            var options = await downloader.GetDownloadOptionsAsync(parsedId, cancellationToken: ct);

            var targetContainer = new Container(container);

            // Find matching option
            VideoDownloadOption? option;
            if (!string.IsNullOrEmpty(quality))
            {
                // Match by container and quality label (e.g. "1080p60", "720p", "360p")
                option = options.FirstOrDefault(o =>
                    o.Container == targetContainer &&
                    o.VideoQuality?.Label?.StartsWith(quality, StringComparison.OrdinalIgnoreCase) == true);

                // Fallback: try exact label match
                option ??= options.FirstOrDefault(o =>
                    o.Container == targetContainer &&
                    string.Equals(o.VideoQuality?.Label, quality, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                // No quality specified: pick the highest quality for the container
                option = options
                    .Where(o => o.Container == targetContainer)
                    .OrderByDescending(o => o.VideoQuality)
                    .FirstOrDefault();
            }

            if (option is null)
                return Results.NotFound(new { error = "No matching download option found." });

            // Get video metadata for the download (needed for subtitles and filename)
            using var youtube = new YoutubeClient();
            var video = await youtube.Videos.GetAsync(parsedId, ct);

            // Download to a temp file
            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{container}");
            await downloader.DownloadVideoAsync(tempPath, video, option, includeSubtitles: false, cancellationToken: ct);

            // Sanitize filename
            var safeTitle = string.Join("_", video.Title.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"{safeTitle}.{container}";

            var contentType = container.ToLowerInvariant() switch
            {
                "mp4" when option.IsAudioOnly => "audio/mp4",
                "mp4" => "video/mp4",
                "webm" when option.IsAudioOnly => "audio/webm",
                "webm" => "video/webm",
                "mp3" => "audio/mpeg",
                "ogg" => "audio/ogg",
                _ => "application/octet-stream"
            };

            // Stream the file back and delete on close
            var stream = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.None,
                bufferSize: 64 * 1024, FileOptions.DeleteOnClose | FileOptions.SequentialScan);

            return Results.File(stream, contentType, fileName);
        });
    }
}
