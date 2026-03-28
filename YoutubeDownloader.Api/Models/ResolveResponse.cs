using System;

namespace YoutubeDownloader.Api.Models;

public record VideoInfo(
    string Id,
    string Title,
    string Author,
    TimeSpan? Duration,
    string? ThumbnailUrl
);

public record ResolveResponse(
    string Kind,
    string Title,
    IReadOnlyList<VideoInfo> Videos
);
