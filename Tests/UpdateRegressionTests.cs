using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HappyColorSplit.Controls;
using HappyColorSplit.Services;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;

internal static class UpdateRegressionTests
{
    private static readonly AvailableUpdate Future = new(999, "v2.0.0", DateTimeOffset.Parse("2026-10-27T10:00:00Z"), GitHubUpdateService.ReleasePage("v2.0.0"));
    public static void Run(string root, Action<bool, string> check, Action<Func<bool>> pump)
    {
        foreach (var (a, b) in new[] { ("0.1.0-preview.9", "0.1.0-preview.10"), ("0.1.0-preview.10", "0.1.0"), ("0.1.9", "0.1.10"), ("0.1.99", "0.2.0"), ("0.9.9", "1.0.0"), ("1.0.0-1", "1.0.0-alpha"), ("1.0.0-alpha", "1.0.0-alpha.1") })
            check(ReleaseVersion.TryParse(a, out var x) && ReleaseVersion.TryParse(b, out var y) && x.CompareTo(y) < 0, "Semantic version order: " + a + " < " + b);
        foreach (string invalid in new[] { "v", "01.0.0", "1.0", "1.0.0-preview.01", "1.0.0\n", "1.0.0/../../x", "1.0.0?x", "1.0.0#x" })
            check(!ReleaseVersion.TryParse(invalid, out _), "Malformed version is rejected: " + invalid);
        check(ReleaseVersion.TryParse("v1.0.0+a", out var buildA) && ReleaseVersion.TryParse("1.0.0+b", out var buildB) && buildA.CompareTo(buildB) == 0, "Build metadata does not cause an update");
        check(ReleaseVersion.TryParse(AppVersion.Current, out _) && !AppVersion.Current.Contains('+'), "Application version is valid and displays without build metadata");

        UpdateCheckResult Read(string releases, string? repo = null, HttpStatusCode code = HttpStatusCode.OK, string current = "0.1.0-preview.5")
        {
            using var client = new HttpClient(new Handler(repo ?? Repo(), releases, code));
            return new GitHubUpdateService(client).CheckAsync(current, CancellationToken.None).GetAwaiter().GetResult();
        }
        string valid = "[" + Release("v0.2.0") + "]";
        check(Read(valid).Update == new AvailableUpdate(999, "v0.2.0", Future.PublishedAt, GitHubUpdateService.ReleasePage("v0.2.0")), "Trusted GitHub metadata offers the newer release with version, publication date and canonical URL");
        check(Read("[" + Release("v0.1.0-preview.5") + "," + Release("v0.1.0-preview.4") + "]").State == UpdateCheckState.UpToDate, "Equal and older releases leave update hidden");
        check(Read("[" + Release("v0.1.0-preview.6", prerelease: true) + "]").State == UpdateCheckState.Available, "Preview installations can see newer preview releases");
        check(Read("[" + Release("v0.2.0-preview.1", prerelease: true) + "]", current: "0.1.0").State == UpdateCheckState.UpToDate, "Stable installations skip preview releases");
        check(Read("[" + Release("v1.1.0-preview.1", prerelease: true) + "]", current: "1.0.0").State == UpdateCheckState.UpToDate, "Version 1.0.0 stays on the stable update channel");
        check(Read("[" + Release("v1.0.1") + "]", current: "1.0.0").Update?.Tag == "v1.0.1", "Version 1.0.0 accepts a newer stable maintenance release");
        check(Read("[" + Release("v0.2.0", draft: true) + "]").State == UpdateCheckState.UpToDate, "Draft releases are never offered");
        check(Read("[" + Release("v0.2.0") + "," + Release("v0.1.0-preview.10", prerelease: true) + "]").Update?.Tag == "v0.2.0", "Semantic version takes precedence over list order");
        check(Read(valid, Repo(repository: GitHubUpdateService.RepositoryId + 1)).State == UpdateCheckState.Untrusted, "Recreated repository with the identical name is rejected by repository ID");
        check(Read(valid, Repo(owner: GitHubUpdateService.OwnerId + 1)).State == UpdateCheckState.Untrusted, "Recreated organization with the identical name is rejected by owner ID");
        check(Read("[" + Release("v0.2.0", author: GitHubUpdateService.PublisherId + 1) + "]").State == UpdateCheckState.Untrusted, "Recreated publisher account with the identical name is rejected by author ID");
        foreach (string url in new[] { "https://github.com.evil.example/HappyProjectLab/HappyColorSplit/releases/tag/v0.2.0", "https://evil.example/file.exe", "http://github.com/HappyProjectLab/HappyColorSplit/releases/tag/v0.2.0", "https://github.com/Other/Repo/releases/tag/v0.2.0", "https://github.com/HappyProjectLab/HappyColorSplit/releases/tag/v0.2.0?next=evil", "file:///C:/untrusted.exe" })
        {
            check(Read("[" + Release("v0.2.0", url: url) + "]").State == UpdateCheckState.Untrusted, "Release URL is rejected: " + url);
            check(!GitHubUpdateService.IsReleasePage(new Uri(url)), "Browser allowlist rejects: " + url);
        }
        check(Read(valid, code: HttpStatusCode.Redirect).State == UpdateCheckState.Unavailable, "HTTP redirects are not followed");
        check(Read(valid, code: HttpStatusCode.Forbidden).State == UpdateCheckState.Unavailable, "GitHub rate limits leave the update hidden");
        check(Read("invalid JSON").State == UpdateCheckState.Unavailable, "Invalid JSON leaves the update hidden");
        check(Read(new string(' ', 2 * 1024 * 1024 + 1)).State == UpdateCheckState.Unavailable, "Oversized GitHub responses are rejected");
        using (var cancelled = new CancellationTokenSource())
        {
            cancelled.Cancel();
            using var client = new HttpClient(new Handler(Repo(), valid));
            check(new GitHubUpdateService(client).CheckAsync(AppVersion.Current, cancelled.Token).GetAwaiter().GetResult().Update is null, "Cancellation never offers a release");
        }

        var updates = new StubUpdates(); var prompt = new StubPrompt(); var browser = new StubBrowser();
        using var vm = new MainViewModel(new Dialogs(), new SettingsService(Path.Combine(root, "update-settings")), windowsIntegration: new Integration(), updateService: updates, updatePrompt: prompt, releaseBrowser: browser);
        var window = new MainWindow { DataContext = vm }; window.Show(); Layout();
        try
        {
            var host = window.FindControl<Grid>("UpdateButtonHost")!;
            var button = window.FindControl<Button>("UpdateButton")!;
            var beacon = window.FindControl<UpdateBeacon>("UpdateButtonBeacon")!;
            check(window.FindControl<TextBlock>("AppVersionLabel")!.Text == "Version " + AppVersion.Current && !host.IsVisible, "Current version is visible before checking; update button is hidden");
            check(updates.Calls == 0, "Opening a test window does not make unrequested network calls");
            updates.Result = new(UpdateCheckState.Available, Future);
            var checking = vm.CheckForUpdatesAsync(); pump(() => checking.IsCompleted); Layout();
            check(host.IsVisible && beacon.IsActive && !beacon.IsHitTestVisible && button.Classes.Contains("toolbar"), "Only a verified newer release shows the toolbar button and nonblocking running light");
            check(vm.UpdateButtonLabel.Contains(Future.Tag) && vm.UpdateButtonLabel.Contains("27.10.2026"), "Update button displays the offered version and publication date in German");
            vm.LanguageIndex = 1; Layout();
            check(vm.UpdateButtonLabel.Contains("2026-10-27"), "Date label updates when switching language");
            vm.ReduceMotion = true; Layout();
            check(beacon.ReduceMotion, "Update running light respects reduced motion");
            foreach (int width in new[] { 1380, 1120 })
            {
                window.Width = width; Layout();
                var selector = window.FindControl<ToggleButton>("WorkspaceSelector")!;
                var location = button.TranslatePoint(default, window)!.Value;
                var right = selector.TranslatePoint(default, window)!.Value;
                check(location.X >= 0 && location.X + button.Bounds.Width <= right.X && button.Bounds.Height == 32, "Update button stays within the title area at window width " + width);
            }
            window.Width = 1380; vm.LanguageIndex = 0; Layout();
            Save(window, "update-button-preview.png");
            prompt.Accept = false;
            vm.OpenUpdateCommand.Execute(null); Layout();
            check(prompt.Calls == 1 && browser.Pages.Count == 0 && updates.Calls == 1, "Declining the browser prompt opens nothing and performs no follow-up request");
            prompt.Accept = true;
            vm.OpenUpdateCommand.Execute(null); pump(() => browser.Pages.Count == 1); Layout();
            check(updates.Calls == 2 && browser.Pages.Single() == Future.Page, "Confirming rechecks trust and opens exactly the canonical release page");
            updates.Result = new(UpdateCheckState.Untrusted);
            vm.OpenUpdateCommand.Execute(null); pump(() => !vm.HasAvailableUpdate); Layout();
            check(browser.Pages.Count == 1 && !host.IsVisible, "Identity change after confirmation hides the update and blocks browser opening");
            updates.Result = new(UpdateCheckState.Available, Future);
            checking = vm.CheckForUpdatesAsync(); pump(() => checking.IsCompleted);
            prompt.OnConfirm = () => updates.Result = new(UpdateCheckState.Available, Future with { ReleaseId = 1000 });
            int calls = updates.Calls;
            vm.OpenUpdateCommand.Execute(null); pump(() => updates.Calls > calls); Layout();
            check(browser.Pages.Count == 1 && vm.HasAvailableUpdate, "Changed release requires a fresh confirmation rather than opening a different offer");
            updates.Result = new(UpdateCheckState.Unavailable);
            checking = vm.CheckForUpdatesAsync(); pump(() => checking.IsCompleted); Layout();
            check(!host.IsVisible && vm.VersionLabel.Contains(AppVersion.Current), "Offline checks keep the version label and hide stale update offers");

            var realPrompt = new UpdatePromptService(window);
            var declined = realPrompt.ConfirmAsync(Future, new Texts("de")); Layout();
            var dialog = window.OwnedWindows.OfType<UpdatePromptWindow>().Single();
            check(dialog.FindControl<Button>("ConfirmUpdateButton")!.Content?.ToString() == "Im Standardbrowser öffnen", "Confirmation names the external default-browser action");
            Save(dialog, "update-browser-prompt.png");
            dialog.FindControl<Button>("CancelUpdateButton")!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); Layout();
            check(declined.IsCompletedSuccessfully && !declined.Result, "Actual confirmation window returns false when cancelled");
        }
        finally { window.Close(); }
    }
    private static string Repo(long repository = GitHubUpdateService.RepositoryId, long owner = GitHubUpdateService.OwnerId) => JsonSerializer.Serialize(new
    { id = repository, owner = new { id = owner }, full_name = GitHubUpdateService.Repository, html_url = "https://github.com/" + GitHubUpdateService.Repository });
    private static string Release(string tag, long author = GitHubUpdateService.PublisherId, string? url = null, bool draft = false, bool prerelease = false) => JsonSerializer.Serialize(new
    { id = 999, tag_name = tag, published_at = "2026-10-27T10:00:00Z", html_url = url ?? GitHubUpdateService.ReleasePage(tag).AbsoluteUri, draft, prerelease, author = new { id = author } });
    private sealed class Handler(string repo, string releases, HttpStatusCode code = HttpStatusCode.OK) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (request.RequestUri!.Host != "api.github.com") throw new Exception("Unexpected host");
            return Task.FromResult(new HttpResponseMessage(code) { RequestMessage = request, Content = new StringContent(request.RequestUri.AbsolutePath.EndsWith("/releases") ? releases : repo, Encoding.UTF8, "application/json") });
        }
    }
    private sealed class StubUpdates : IUpdateService
    {
        public int Calls;
        public UpdateCheckResult Result = new(UpdateCheckState.UpToDate);
        public Task<UpdateCheckResult> CheckAsync(string version, CancellationToken token) { Calls++; return Task.FromResult(Result); }
    }
    private sealed class StubPrompt : IUpdatePromptService
    {
        public int Calls; public bool Accept; public Action? OnConfirm;
        public Task<bool> ConfirmAsync(AvailableUpdate update, Texts texts) { Calls++; if (Accept) OnConfirm?.Invoke(); return Task.FromResult(Accept); }
    }
    private sealed class StubBrowser : IReleaseBrowser
    {
        public List<Uri> Pages { get; } = [];
        public void Open(Uri page) => Pages.Add(page);
    }
    private sealed class Dialogs : IFileDialogService
    {
        public Task<IReadOnlyList<string>> OpenImagesAsync(Texts texts) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<string?> SelectOutputAsync(Texts texts) => Task.FromResult<string?>(null);
    }
    private sealed class Integration : IWindowsIntegrationService
    {
        public bool IsAvailable => true;
        public void Enable() { }
        public void Remove() { }
    }
    private static void Layout() => Dispatcher.UIThread.RunJobs();
    private static void Save(Window window, string name)
    {
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height));
        bitmap.Render(window);
        string folder = Path.Combine(Directory.GetCurrentDirectory(), "artifacts"); Directory.CreateDirectory(folder);
        using var output = File.Create(Path.Combine(folder, name)); bitmap.Save(output, PngBitmapEncoderOptions.Default);
    }
}
