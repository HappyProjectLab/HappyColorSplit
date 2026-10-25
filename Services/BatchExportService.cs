using HappyColorSplit.Models;

namespace HappyColorSplit.Services;

public sealed class BatchExportService
{
    public BatchExportResult Export(IReadOnlyList<string> paths, string parent, double tolerance,
        IProgress<BatchProgress>? progress, CancellationToken token, byte minimumAlpha = 1,
        IReadOnlyDictionary<string, IReadOnlySet<string>>? exportColors = null, ExportOptions? options = null,
        IReadOnlyDictionary<string, ImageScale>? imageScales = null, bool combineGroups = false)
    {
        token.ThrowIfCancellationRequested();
        string folder = Path.Combine(parent, $"HappyColorSplit_{(combineGroups ? "Merged" : "Batch")}_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}");
        Directory.CreateDirectory(folder);
        var failures = new List<BatchFailure>();
        int exportedImages = 0, exportedMasks = 0, skippedImages = 0;
        bool cancelled = false;
        var service = new ColorSeparationService();
        // Decode and analyze one image at a time to bound batch memory use.
        for (int i = 0; i < paths.Count; i++)
        {
            if (token.IsCancellationRequested) { cancelled = true; break; }
            int fileIndex = i;
            void Report(int filePercent) => progress?.Report(new(fileIndex + 1, paths.Count,
                (fileIndex * 100 + filePercent) / paths.Count, Path.GetFileName(paths[fileIndex])));
            Report(0);
            if (exportColors is not null && (!exportColors.TryGetValue(paths[i], out var selectedColors) || selectedColors.Count == 0))
            {
                skippedImages++; Report(100); continue;
            }
            try
            {
                using var image = service.Load(paths[i]);
                token.ThrowIfCancellationRequested();
                var analysis = service.Analyze(image, tolerance, token, minimumAlpha);
                IReadOnlySet<int>? selected = exportColors is not null && exportColors.TryGetValue(paths[i], out var colors)
                    ? analysis.Groups.Where(g => colors.Contains(g.Hex)).Select(g => g.Index).ToHashSet() : null;
                var sizing = imageScales is not null && imageScales.TryGetValue(paths[i], out var scale) ? scale : null;
                service.Export(analysis, folder, paths[i], new InlineProgress<int>(Report), token, selected, options, sizing, combineGroups);
                exportedImages++;
                exportedMasks += combineGroups ? 1 : selected?.Count ?? analysis.Groups.Count;
            }
            catch (OperationCanceledException) { cancelled = true; break; }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                failures.Add(new(paths[i], error));
            }
            Report(100);
        }
        return new(folder, exportedImages, exportedMasks, failures, cancelled, skippedImages);
    }

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
