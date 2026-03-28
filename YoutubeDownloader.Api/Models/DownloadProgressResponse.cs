namespace YoutubeDownloader.Api.Models;

public record DownloadProgressResponse(
    string ProgressId,
    string Status,
    double Progress,
    string? Message,
    string? FileName,
    string? Error,
    IReadOnlyList<string> Warnings
);
