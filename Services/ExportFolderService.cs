using System.Diagnostics;

namespace HappyColorSplit.Services;

public interface IExportFolderService
{
    void Open(string folder);
}

public sealed class ExportFolderService : IExportFolderService
{
    public void Open(string folder) => Process.Start(new ProcessStartInfo
    {
        FileName = folder,
        UseShellExecute = true
    });
}
