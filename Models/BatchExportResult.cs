namespace HappyColorSplit.Models;

public sealed record BatchFailure(string FilePath, Exception Error);
public sealed record BatchProgress(int FileNumber, int TotalFiles, int Percent, string FileName);
public sealed record BatchExportResult(string Folder, int ExportedImages, int ExportedMasks,
    IReadOnlyList<BatchFailure> Failures, bool Cancelled);
