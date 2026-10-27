using System.Diagnostics;
using Avalonia.Controls;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;

namespace HappyColorSplit.Services;

public interface IUpdatePromptService
{
    Task<bool> ConfirmAsync(AvailableUpdate update, Texts texts);
}
public interface IReleaseBrowser
{
    void Open(Uri page);
}
public sealed class ReleaseBrowser : IReleaseBrowser
{
    public void Open(Uri page)
    {
        if (!GitHubUpdateService.IsReleasePage(page)) throw new InvalidOperationException("Untrusted release URL.");
        Process.Start(new ProcessStartInfo(page.AbsoluteUri) { UseShellExecute = true });
    }
}
public sealed class UpdatePromptService(Window owner) : IUpdatePromptService
{
    public async Task<bool> ConfirmAsync(AvailableUpdate update, Texts texts) => await new UpdatePromptWindow
    { DataContext = new UpdatePromptViewModel(texts, update) }.ShowDialog<bool>(owner);
}

public sealed class UpdatePromptViewModel(Texts texts, AvailableUpdate update)
{
    public Texts Text { get; } = texts;
    public string ReleaseLabel => string.Format(Text["UpdateAvailableButton"], update.Tag, UpdateDate.Format(update.PublishedAt, Text.Language));
    public string ReleasePage => update.Page.AbsoluteUri;
}

internal static class UpdateDate
{
    public static string Format(DateTimeOffset date, string language) => date.ToLocalTime().ToString(language == "de" ? "dd.MM.yyyy" : "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}
