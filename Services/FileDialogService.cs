using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace HappyColorSplit.Services;

public interface IFileDialogService
{
    Task<IReadOnlyList<string>> OpenImagesAsync(Texts texts);
    Task<string?> SelectOutputAsync(Texts texts);
}

public sealed class FileDialogService(Window window) : IFileDialogService
{
    public async Task<IReadOnlyList<string>> OpenImagesAsync(Texts texts)
    {
        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = texts["Open"], AllowMultiple = true,
            FileTypeFilter = [new FilePickerFileType(texts["Images"]) { Patterns = SupportedImageFiles.Extensions.Select(ext => "*" + ext).ToArray(), MimeTypes = ["image/png", "image/jpeg"] }]
        });
        return files.Select(file => file.TryGetLocalPath()).OfType<string>().ToArray();
    }

    public async Task<string?> SelectOutputAsync(Texts texts)
    {
        var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = texts["Folder"], AllowMultiple = false });
        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }
}
