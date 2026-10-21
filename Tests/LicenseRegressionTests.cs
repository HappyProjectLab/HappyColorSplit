using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HappyColorSplit.Models;
using HappyColorSplit.Services;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;
using System.Text.Json;

internal static class LicenseRegressionTests
{
    public static void Run(Window owner, string testDirectory, Action<bool, string> check, Action<Func<bool>> pump)
    {
        var service = new LicenseDocumentService();
        var de = service.GetDocuments(new Texts("de"));
        var en = service.GetDocuments(new Texts("en"));
        check(de.Count == 21 && en.Count == 21, "License catalog exposes all documents in both UI languages");
        check(de.Single(d => d.RelativePath.Contains("USAGE_NOTICE")).RelativePath.EndsWith("_DE.md") &&
            en.Single(d => d.RelativePath.Contains("USAGE_NOTICE")).RelativePath.EndsWith("USAGE_NOTICE.md"), "Usage notice follows UI language");
        check(de.Single(d => d.RelativePath.Contains("COMPONENTS")).RelativePath.EndsWith("_DE.md") &&
            en.Single(d => d.RelativePath.Contains("COMPONENTS")).RelativePath.EndsWith("COMPONENTS.md"), "License overview follows UI language");
        using (var inventory = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "licenses", "component-inventory.json"))))
        {
            var required = inventory.RootElement.GetProperty("upstreamLegalFiles").EnumerateArray()
                .Select(file => file.GetProperty("path").GetString()!).ToHashSet();
            check(required.IsSubsetOf(de.Select(document => document.RelativePath)), "Every inventoried upstream legal text is accessible in the viewer");
        }

        using (var vm = new LicensesViewModel(new Texts("de"), service))
        {
            pump(() => !vm.IsLoading);
            check(vm.DocumentText.Contains("GNU GENERAL PUBLIC LICENSE"), "Viewer initially displays full project license");
            foreach (var document in vm.Documents)
            {
                vm.SelectedDocument = document;
                pump(() => !vm.IsLoading);
                string original = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, document.RelativePath));
                check(vm.DocumentText == original, "Viewer preserves complete original text: " + document.RelativePath);
            }
        }

        using (var english = new LicensesViewModel(new Texts("en"), service))
        {
            english.SelectedDocument = en.Single(d => d.RelativePath.Contains("COMPONENTS"));
            pump(() => !english.IsLoading);
            check(english.Title.Contains("Licenses & notices") && english.DocumentText.StartsWith("# Third-party components"), "English viewer loads English overview");
            english.SelectedDocument = en.Single(d => d.RelativePath.Contains("USAGE_NOTICE"));
            pump(() => !english.IsLoading);
            check(english.DocumentText.Contains("Mandatory statutory rights remain unaffected.") && english.DocumentText.Contains("back up important files"), "English usage notice preserves statutory rights and explains backups");
        }
        using (var missing = new LicensesViewModel(new Texts("de"), new LicenseDocumentService(Path.Combine(testDirectory, "missing-license-files"))))
        {
            pump(() => !missing.IsLoading);
            check(missing.DocumentText.Contains("vollständige Programmpaket"), "Missing license produces actionable localized message");
        }

        var delayed = new DelayedLicenseDocuments();
        using (var latest = new LicensesViewModel(new Texts("en"), delayed))
        {
            latest.SelectedDocument = latest.Documents[1];
            check(latest.DocumentText == "new document", "New selection loads without waiting for prior document");
            delayed.First.SetResult("old document");
            pump(() => delayed.CompletedReads == 2);
            check(latest.DocumentText == "new document" && !latest.IsLoading, "Late result cannot overwrite a more recent license selection");
        }

        var windowService = new LicenseWindowService(owner);
        Task closed = windowService.ShowAsync(new Texts("de"));
        var window = owner.OwnedWindows.OfType<LicensesWindow>().Single();
        var windowVm = (LicensesViewModel)window.DataContext!;
        pump(() => !windowVm.IsLoading);
        var textBox = window.FindControl<TextBox>("LicenseTextBox")!;
        check(window.IsVisible && textBox.IsReadOnly && textBox.Text!.Contains("GNU GENERAL PUBLIC LICENSE"), "Real license window displays selectable original license");
        windowVm.SelectedDocument = windowVm.Documents.Single(d => d.RelativePath == "licenses/DotNet-AppHost/THIRD-PARTY-NOTICES.TXT");
        pump(() => !windowVm.IsLoading);
        check(textBox.Text == windowVm.DocumentText && textBox.Text!.Length > 70_000, "Long third-party notice is displayed without truncation");
        var close = window.GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Content, "Schließen"));
        close.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        pump(() => closed.IsCompleted);
        closed.GetAwaiter().GetResult();
        check(!window.IsVisible && owner.IsVisible, "Close button returns to the main window");
        Dispatcher.UIThread.RunJobs();
    }

    private sealed class DelayedLicenseDocuments : ILicenseDocumentService
    {
        public TaskCompletionSource<string> First { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int CompletedReads { get; private set; }
        public IReadOnlyList<LicenseDocument> GetDocuments(Texts texts) => [new("First", "first"), new("Second", "second")];
        public async Task<string> ReadAsync(LicenseDocument document, CancellationToken token)
        {
            // Intentionally ignore cancellation to verify protection against a late completion.
            string text = document.RelativePath == "first" ? await First.Task : "new document";
            CompletedReads++;
            return text;
        }
    }
}
