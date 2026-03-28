using System.ComponentModel;

namespace YoutubeDownloader.Api.Models;

public sealed class AudioDownloadRequest
{
    [Description("Optional output container such as mp3, webm, ogg, or mp4. Defaults to mp3.")]
    public string? Container { get; init; }

    [Description("Optional quality label. Audio downloads typically ignore this unless the selected container exposes multiple options.")]
    public string? Quality { get; init; }

    [Description("When true, includes subtitles as a generated .srt file. Defaults to true. The response becomes a .zip archive when this option is enabled.")]
    public bool? IncludeSrt { get; init; }

    [Description("When true, includes the video description as description.txt. Defaults to true. The response becomes a .zip archive when this option is enabled.")]
    public bool? IncludeDescription { get; init; }

    [Description("Optional subtitle language code or language name to use when generating the .srt file, for example 'en'.")]
    public string? SubtitleLanguage { get; init; }

    [Description("Optional client-generated ID used to poll /api/downloads/progress/{progressId} while the file is being prepared.")]
    public string? ProgressId { get; init; }
}
