namespace YoutubeDownloader.Api.Models;

public record DownloadOptionInfo(
    string Container,
    bool IsAudioOnly,
    string? VideoQuality
);
