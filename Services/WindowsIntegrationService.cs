using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace HappyColorSplit.Services;

public interface IWindowsIntegrationService
{
    bool IsAvailable { get; }
    void Enable();
    void Remove();
}

public sealed class WindowsIntegrationService : IWindowsIntegrationService
{
    private const string ProgId = "HappyColorSplit.Image";
    private const string ApplicationKey = @"Software\Classes\Applications\HappyColorSplit.exe";
    public bool IsAvailable => OperatingSystem.IsWindows()
        && string.Equals(Path.GetFileName(Environment.ProcessPath), "HappyColorSplit.exe", StringComparison.OrdinalIgnoreCase);

    public void Enable()
    {
        if (!OperatingSystem.IsWindows() || !IsAvailable) throw new PlatformNotSupportedException();
        Register(Environment.ProcessPath!, Registry.CurrentUser, ShortcutPath);
    }

    public void Remove()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        Unregister(Registry.CurrentUser, ShortcutPath);
    }

    [SupportedOSPlatform("windows")]
    internal static void Register(string executable, RegistryKey userRoot, string shortcutPath)
    {
        // Per-user registration only. Never change an extension's default value or UserChoice.
        string command = $"\"{executable}\" \"%1\"";
        using (var app = userRoot.CreateSubKey(ApplicationKey))
        {
            app.SetValue("FriendlyAppName", "HappyColorSplit");
            using var icon = app.CreateSubKey("DefaultIcon");
            icon.SetValue("", $"\"{executable}\",0");
            using var open = app.CreateSubKey(@"shell\open\command");
            open.SetValue("", command);
            using var types = app.CreateSubKey("SupportedTypes");
            foreach (string extension in SupportedImageFiles.Extensions) types.SetValue(extension, "");
        }
        using (var image = userRoot.CreateSubKey(@"Software\Classes\" + ProgId))
        {
            image.SetValue("", "HappyColorSplit image");
            using var icon = image.CreateSubKey("DefaultIcon");
            icon.SetValue("", $"\"{executable}\",0");
            using var open = image.CreateSubKey(@"shell\open\command");
            open.SetValue("", command);
        }
        foreach (string extension in SupportedImageFiles.Extensions)
        {
            using var choices = userRoot.CreateSubKey(@"Software\Classes\" + extension + @"\OpenWithProgids");
            choices.SetValue(ProgId, Array.Empty<byte>(), RegistryValueKind.None);
        }
        CreateSendToShortcut(executable, shortcutPath);
        NotifyExplorer();
    }

    [SupportedOSPlatform("windows")]
    internal static void Unregister(RegistryKey userRoot, string shortcutPath)
    {
        // Delete only this application's own registrations, retaining other applications' entries.
        foreach (string extension in SupportedImageFiles.Extensions)
        {
            using var choices = userRoot.OpenSubKey(@"Software\Classes\" + extension + @"\OpenWithProgids", true);
            choices?.DeleteValue(ProgId, false);
        }
        userRoot.DeleteSubKeyTree(@"Software\Classes\" + ProgId, false);
        userRoot.DeleteSubKeyTree(ApplicationKey, false);
        File.Delete(shortcutPath);
        NotifyExplorer();
    }

    [SupportedOSPlatform("windows")]
    private static string ShortcutPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SendTo), "HappyColorSplit.lnk");

    [SupportedOSPlatform("windows")]
    private static void CreateSendToShortcut(string executable, string shortcutPath)
    {
        object? shell = null, shortcut = null;
        try
        {
            shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell", throwOnError: true)!);
            shortcut = ((dynamic)shell!).CreateShortcut(shortcutPath);
            dynamic link = shortcut;
            link.TargetPath = executable;
            link.Arguments = "";
            link.WorkingDirectory = Path.GetDirectoryName(executable)!;
            link.IconLocation = executable + ",0";
            link.Description = "HappyColorSplit (PNG / JPG / JPEG)";
            link.Save();
        }
        finally
        {
            if (shortcut is not null) Marshal.FinalReleaseComObject(shortcut);
            if (shell is not null) Marshal.FinalReleaseComObject(shell);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void NotifyExplorer() => SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint eventId, uint flags, IntPtr item1, IntPtr item2);
}
