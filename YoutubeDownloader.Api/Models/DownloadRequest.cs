using System.ComponentModel;

namespace YoutubeDownloader.Api.Models;

public sealed class DownloadRequest
{
    [Description("Optional output container such as mp4, webm, mp3, or ogg. Defaults to mp4 for video downloads and mp3 for audio-only downloads.")]
    public string? Container { get; init; }

    [Description("Optional quality label such as 1080p60, 720p, or 360p. If omitted, the highest quality for the selected container is used.")]
    public string? Quality { get; init; }

    [Description("When true, selects an audio-only download option. If no container is provided, mp3 is used by default.")]
    public bool AudioOnly { get; init; }

    [Description("When true, includes subtitles as a generated .srt file. Defaults to true. The response becomes a .zip archive when this option is enabled.")]
    public bool? IncludeSrt { get; init; }

    [Description("When true, includes the video description as description.txt. Defaults to true. The response becomes a .zip archive when this option is enabled.")]
    public bool? IncludeDescription { get; init; }

    [Description("Optional subtitle language code or language name to use when generating the .srt file, for example 'en'.")]
    public string? SubtitleLanguage { get; init; }

    [Description("Optional client-generated ID used to poll /api/downloads/progress/{progressId} while the file is being prepared.")]
    public string? ProgressId { get; init; }
}
