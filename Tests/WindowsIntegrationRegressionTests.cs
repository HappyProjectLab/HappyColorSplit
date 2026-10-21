using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using HappyColorSplit.Services;
using Microsoft.Win32;

internal static class WindowsIntegrationRegressionTests
{
    [SupportedOSPlatform("windows")]
    public static void Run(string folder, Action<bool, string> check)
    {
        // Isolated test subtree: no actual Explorer associations or SendTo directory are changed.
        string testKey = @"Software\HappyProjectLab\HappyColorSplit\Tests\" + Guid.NewGuid().ToString("N");
        try
        {
            using var root = Registry.CurrentUser.CreateSubKey(testKey);
            using (var png = root.CreateSubKey(@"Software\Classes\.png")) png.SetValue("", "Existing.ImageViewer");
            using (var other = root.CreateSubKey(@"Software\Classes\.png\OpenWithProgids")) other.SetValue("Other.Application", "keep");
            using (var choice = root.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.png\UserChoice"))
                choice.SetValue("ProgId", "Existing.ImageViewer");
            string shortcut = Path.Combine(folder, "HappyColorSplit integration test.lnk");
            string executable = Path.Combine(folder, "program folder ä", "HappyColorSplit.exe");
            WindowsIntegrationService.Register(executable, root, shortcut);
            using (var command = root.OpenSubKey(@"Software\Classes\Applications\HappyColorSplit.exe\shell\open\command"))
                check(Equals(command?.GetValue(""), $"\"{executable}\" \"%1\""), "Explorer executable and image argument are individually quoted");
            using (var supported = root.OpenSubKey(@"Software\Classes\Applications\HappyColorSplit.exe\SupportedTypes"))
                check(supported!.GetValueNames().Order().SequenceEqual(new[] { ".jpeg", ".jpg", ".png" }), "Open-with registration advertises only PNG, JPG and JPEG");
            using (var png = root.OpenSubKey(@"Software\Classes\.png"))
                check(Equals(png?.GetValue(""), "Existing.ImageViewer"), "Registering leaves the current default image viewer unchanged");
            using (var choice = root.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.png\UserChoice"))
                check(Equals(choice?.GetValue("ProgId"), "Existing.ImageViewer"), "Registering leaves Windows UserChoice untouched");
            check(root.OpenSubKey(@"Software\Classes\.gif") is null, "No association is registered for unsupported extensions");
            CheckShortcut(shortcut, executable, check);
            string moved = Path.Combine(folder, "new location", "HappyColorSplit.exe");
            WindowsIntegrationService.Register(moved, root, shortcut);
            CheckShortcut(shortcut, moved, check);
            WindowsIntegrationService.Unregister(root, shortcut);
            check(!File.Exists(shortcut) && root.OpenSubKey(@"Software\Classes\HappyColorSplit.Image") is null
                && root.OpenSubKey(@"Software\Classes\Applications\HappyColorSplit.exe") is null,
                "Removing Windows integration deletes only the application's entries and shortcut");
            using (var choices = root.OpenSubKey(@"Software\Classes\.png\OpenWithProgids"))
                check(Equals(choices?.GetValue("Other.Application"), "keep") && choices?.GetValue("HappyColorSplit.Image") is null,
                    "Removing integration retains another application's open-with entry");
            WindowsIntegrationService.Unregister(root, shortcut);
            check(true, "Removing already-removed Explorer entries is safe");
        }
        finally { Registry.CurrentUser.DeleteSubKeyTree(testKey, false); }
    }

    [SupportedOSPlatform("windows")]
    private static void CheckShortcut(string path, string executable, Action<bool, string> check)
    {
        object? shell = null, shortcut = null;
        try
        {
            shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell", true)!);
            shortcut = ((dynamic)shell!).CreateShortcut(path);
            dynamic link = shortcut;
            check(string.Equals((string)link.TargetPath, executable, StringComparison.OrdinalIgnoreCase)
                && (string)link.Arguments == "" && (string)link.WorkingDirectory == Path.GetDirectoryName(executable),
                "Actual SendTo shortcut targets the correct executable, preserves spaces/Unicode and has no fixed image arguments");
        }
        finally
        {
            if (shortcut is not null) Marshal.FinalReleaseComObject(shortcut);
            if (shell is not null) Marshal.FinalReleaseComObject(shell);
        }
    }
}
