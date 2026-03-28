namespace YoutubeDownloader.Api.Models;

public record DownloadLinkResponse(
    string Url,
    string FileName,
    string ContentType,
    string Container,
    bool IsAudioOnly,
    string? VideoQuality
);
