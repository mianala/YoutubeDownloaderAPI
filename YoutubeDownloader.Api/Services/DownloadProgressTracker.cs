using System.Collections.Concurrent;
using YoutubeDownloader.Api.Models;

namespace YoutubeDownloader.Api.Services;

public sealed class DownloadProgressTracker
{
    private readonly ConcurrentDictionary<string, DownloadProgressResponse> _downloads = new();

    public DownloadProgressResponse Start(string progressId, string? message = null) =>
        _downloads[progressId] = new DownloadProgressResponse(
            progressId,
            "started",
            0,
            message,
            null,
            null,
            []
        );

    public void Report(
        string progressId,
        string status,
        double progress,
        string? message = null,
        string? fileName = null
    )
    {
        _downloads.AddOrUpdate(
            progressId,
            _ => new DownloadProgressResponse(
                progressId,
                status,
                Clamp(progress),
                message,
                fileName,
                null,
                []
            ),
            (_, current) => current with
            {
                Status = status,
                Progress = Clamp(progress),
                Message = message ?? current.Message,
                FileName = fileName ?? current.FileName
            }
        );
    }

    public void AddWarning(string progressId, string warning)
    {
        _downloads.AddOrUpdate(
            progressId,
            _ => new DownloadProgressResponse(
                progressId,
                "warning",
                0,
                warning,
                null,
                null,
                [warning]
            ),
            (_, current) => current with
            {
                Warnings = [.. current.Warnings, warning],
                Message = current.Message ?? warning
            }
        );
    }

    public void Complete(string progressId, string? fileName = null, IReadOnlyList<string>? warnings = null)
    {
        _downloads.AddOrUpdate(
            progressId,
            _ => new DownloadProgressResponse(
                progressId,
                "completed",
                1,
                "Download prepared.",
                fileName,
                null,
                warnings ?? []
            ),
            (_, current) => current with
            {
                Status = "completed",
                Progress = 1,
                Message = "Download prepared.",
                FileName = fileName ?? current.FileName,
                Warnings = warnings ?? current.Warnings
            }
        );
    }

    public void Fail(string progressId, string error)
    {
        _downloads.AddOrUpdate(
            progressId,
            _ => new DownloadProgressResponse(
                progressId,
                "failed",
                1,
                error,
                null,
                error,
                []
            ),
            (_, current) => current with
            {
                Status = "failed",
                Progress = 1,
                Message = error,
                Error = error
            }
        );
    }

    public DownloadProgressResponse? Get(string progressId) =>
        _downloads.TryGetValue(progressId, out var response) ? response : null;

    private static double Clamp(double progress) => Math.Clamp(progress, 0, 1);
}
