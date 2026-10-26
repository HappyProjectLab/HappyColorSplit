using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using HappyColorSplit.Models;
using HappyColorSplit.Services;

namespace HappyColorSplit.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IFileDialogService _dialogs;
    private readonly SettingsService _settingsService;
    private readonly AppSettings _settings;
    private readonly IDetailPreviewService? _detailPreview;
    private readonly ILicenseWindowService? _licenseWindow;
    private readonly IWindowsIntegrationService _windowsIntegration;
    private readonly IExportFolderService _exportFolderService;
    private readonly ColorSeparationService _service = new();
    private RasterImage? _source;
    private ColorAnalysis? _analysis;
    private CancellationTokenSource? _cancellation;
    private Bitmap? _original, _mask;
    private bool _busy, _licensesOpen, _importing;
    private bool _showCombinedPreview, _updatingColorSelection;
    private string _filePath = "", _statusKey = "Ready";
    private (string Key, object[] Args)? _integrationFeedback;
    private object[] _statusArgs = [];
    private int _progress;
    private ColorGroupViewModel? _selected;
    private ImageFileViewModel? _selectedImage;
    private IReadOnlyList<BatchFailure> _failures = [];

    public MainViewModel(IFileDialogService dialogs, SettingsService settingsService, IDetailPreviewService? detailPreview = null,
        ILicenseWindowService? licenseWindow = null, IWindowsIntegrationService? windowsIntegration = null,
        IExportFolderService? exportFolderService = null)
    {
        _dialogs = dialogs; _settingsService = settingsService; _settings = settingsService.Load(); _detailPreview = detailPreview;
        _licenseWindow = licenseWindow;
        _windowsIntegration = windowsIntegration ?? new WindowsIntegrationService();
        _exportFolderService = exportFolderService ?? new ExportFolderService();
        OpenCommand = new(OpenAsync, () => !IsBusy);
        AnalyzeCommand = new(AnalyzeAsync, () => !IsBusy && Images.Count > 0);
        ExportCommand = new(ExportAsync, () => !IsBusy && _analysis is not null && ExportColorCount > 0);
        ExportBatchCommand = new(ExportBatchAsync, () => !IsBusy && HasBatchSelection);
        ExportCombinedBatchCommand = new(() => ExportBatchAsync(true), () => !IsBusy && HasBatchSelection);
        ClearImagesCommand = new(ClearImagesAsync, () => !IsBusy && Images.Count > 0);
        RemoveImageCommand = new(RemoveImageAsync, () => !IsBusy && SelectedImage is not null);
        CancelCommand = new(() => { Cancel(); return Task.CompletedTask; }, () => IsBusy);
        DetailPreviewCommand = new(ShowDetailPreviewAsync, () => !IsBusy && _analysis is not null && HasMask && _detailPreview is not null);
        SelectAllColorsCommand = new(() => SelectColors(true), () => !IsBusy && HasGroups);
        DeselectAllColorsCommand = new(() => SelectColors(false), () => !IsBusy && HasGroups);
        LicensesCommand = new(ShowLicensesAsync, () => _licenseWindow is not null);
        EnableWindowsIntegrationCommand = new(() => UpdateWindowsIntegrationAsync(true), () => IsIdle && CanIntegrateWithWindows);
        RemoveWindowsIntegrationCommand = new(() => UpdateWindowsIntegrationAsync(false), () => IsIdle && CanIntegrateWithWindows);
        OpenLastExportFolderCommand = new(OpenLastExportFolderAsync, () => CanOpenLastExportFolder);
        ResetScaleCommand = new(() => { ApplyScale(new(100, (int)ScaleDpi)); return Task.CompletedTask; }, () => IsIdle && HasImage);
        TripleContourResolutionCommand = new(TripleContourResolutionAsync, () => CanTripleContourResolution);
        Laser = new(this, _settings.Laser);
        InitializeWorkspacePreferences();
    }

    public Texts Text => new(_settings.Language);
    public string[] Languages { get; } = ["Deutsch", "English"];
    public int LanguageIndex
    {
        get => _settings.Language == "en" ? 1 : 0;
        set { _settings.Language = value == 1 ? "en" : "de"; Changed(); Changed(nameof(EnglishSelected)); Changed(nameof(Text)); Changed(nameof(Status)); Changed(nameof(WindowsIntegrationStatus)); Changed(nameof(ErrorDetails)); Changed(nameof(ExportOutputHelp)); Changed(nameof(ImageSizeSummary)); Laser.ChangeLanguage(); RefreshWorkspaceLabels(); SaveSettings(); }
    }
    public bool EnglishSelected { get => LanguageIndex == 1; set => LanguageIndex = value ? 1 : 0; }
    public bool IsLicenseWindowOpen { get => _licensesOpen; private set => Set(ref _licensesOpen, value); }
    public string WindowsIntegrationStatus => _integrationFeedback is { } feedback ? string.Format(Text[feedback.Key], feedback.Args) : "";
    public double Tolerance
    {
        get => _settings.Tolerance;
        set
        {
            value = Math.Clamp(Math.Round(value), 0, 150);
            if (_settings.Tolerance == value) return;
            _settings.Tolerance = value; Changed();
            InvalidateSelections();
            if (_source is not null) StatusMessage("Changed");
        }
    }
    public double MinimumAlpha
    {
        get => _settings.MinimumAlpha;
        set
        {
            int alpha = (int)Math.Clamp(Math.Round(value), 1, 254);
            if (_settings.MinimumAlpha == alpha) return;
            _settings.MinimumAlpha = alpha; Changed(); InvalidateSelections();
            if (_source is not null) StatusMessage("Changed");
        }
    }
    public int ExportColorCount => Groups.Count(group => group.ExportEnabled);
    public bool ShowCombinedPreview
    {
        get => _showCombinedPreview;
        set { if (Set(ref _showCombinedPreview, value)) { RefreshMask(); DetailPreviewCommand.Refresh(); } }
    }
    public bool HasBatchSelection => Images.Any(image => image.ColorsDetected && image.ExportColors is { Count: > 0 });
    public int ExportAreaIndex
    {
        get => (int)_settings.ExportArea;
        set
        {
            if (!Enum.IsDefined((ExportArea)value) || ExportAreaIndex == value) return;
            _settings.ExportArea = (ExportArea)value; Changed(); Changed(nameof(ExportOutputHelp));
            RefreshMask(); Laser.RefreshAppearance(); SaveSettings();
        }
    }
    public int ExportBackgroundIndex
    {
        get => (int)_settings.ExportBackground;
        set
        {
            if (!Enum.IsDefined((ExportBackground)value) || ExportBackgroundIndex == value) return;
            _settings.ExportBackground = (ExportBackground)value;
            Changed(); Changed(nameof(ExportOutputHelp)); RefreshMask(); Laser.RefreshAppearance(); SaveSettings();
        }
    }
    public string ExportOutputHelp => string.Format(Text["ExportAppearanceHelp"],
        Text["Area" + _settings.ExportArea], Text["Background" + _settings.ExportBackground]);
    public bool SmoothContours
    {
        get => _settings.SmoothContours;
        set
        {
            if (IsBusy || value == _settings.SmoothContours) return;
            _settings.SmoothContours = value; Changed(); Laser.Invalidate(); RefreshMask(); SaveSettings();
        }
    }
    private ExportOptions CurrentExportOptions => new(_settings.ExportArea, _settings.ExportBackground, _settings.SmoothContours);
    public int ScaleUnitIndex
    {
        get => SelectedImage?.ScaleUnit ?? _settings.ScaleUnit;
        set
        {
            if (value is not (0 or 1) || value == ScaleUnitIndex) return;
            if (SelectedImage is not null) SelectedImage.ScaleUnit = value;
            _settings.ScaleUnit = value; Changed(); Changed(nameof(IsScalePercent)); Changed(nameof(IsScaleCm)); SaveSettings();
        }
    }
    public bool IsScalePercent => ScaleUnitIndex == 0;
    public bool IsScaleCm => ScaleUnitIndex == 1;
    public decimal ScalePercent
    {
        get => SelectedImage?.Scale.Percent ?? 100;
        set => ApplyScale(new(value, (int)ScaleDpi));
    }
    public decimal ScaleWidthCm
    {
        get => SelectedImage?.Scale.WidthCm(SelectedImage.SourceWidth) ?? 0;
        set { if (SelectedImage is not null) ApplyScale(new(value * ScaleDpi * 100 / (2.54m * SelectedImage.SourceWidth), (int)ScaleDpi)); }
    }
    public decimal ScaleHeightCm
    {
        get => SelectedImage?.Scale.HeightCm(SelectedImage.SourceHeight) ?? 0;
        set { if (SelectedImage is not null) ApplyScale(new(value * ScaleDpi * 100 / (2.54m * SelectedImage.SourceHeight), (int)ScaleDpi)); }
    }
    public decimal ScaleDpi
    {
        get => SelectedImage?.Scale.Dpi ?? _settings.DefaultExportDpi;
        set
        {
            if (value != decimal.Truncate(value) || value is < 1 or > 2400) { RefreshScaleProperties(); return; }
            int dpi = (int)value;
            if (dpi == (int)ScaleDpi || SelectedImage is null) return;
            decimal percent = IsScaleCm ? ScalePercent * dpi / ScaleDpi : ScalePercent;
            if (ApplyScale(new(percent, dpi))) { _settings.DefaultExportDpi = dpi; SaveSettings(); }
        }
    }
    public string ImageSizeSummary
    {
        get
        {
            if (SelectedImage is null) return "";
            var image = SelectedImage;
            var size = image.Scale.GetSize(image.SourceWidth, image.SourceHeight);
            return string.Format(Text["ImageSizeSummary"], image.SourceWidth, image.SourceHeight, size.Width, size.Height,
                size.Width * 2.54m / image.Scale.Dpi, size.Height * 2.54m / image.Scale.Dpi, image.Scale.Dpi);
        }
    }
    private bool ApplyScale(ImageScale sizing)
    {
        if (IsBusy || SelectedImage is null || SelectedImage.Scale == sizing) return false;
        try
        {
            sizing.GetSize(SelectedImage.SourceWidth, SelectedImage.SourceHeight);
            SelectedImage.Scale = sizing;
            Laser.Invalidate();
            RefreshScaleProperties();
            if (_source is not null)
            {
                using var stream = new MemoryStream(_service.OriginalPreview(_source, sizing: sizing));
                Original = new Bitmap(stream);
            }
            RefreshMask(); StatusMessage("ScaleChanged"); return true;
        }
        catch (ExportSizeLimitException) { StatusMessage("ExportTooLarge"); }
        catch (ArgumentOutOfRangeException) { StatusMessage("InvalidSize"); }
        finally { RefreshScaleProperties(); }
        return false;
    }
    private void RefreshScaleProperties()
    {
        Changed(nameof(ScaleUnitIndex)); Changed(nameof(IsScalePercent)); Changed(nameof(IsScaleCm));
        Changed(nameof(ScalePercent)); Changed(nameof(ScaleWidthCm)); Changed(nameof(ScaleHeightCm));
        Changed(nameof(ScaleDpi)); Changed(nameof(ImageSizeSummary));
        TripleContourResolutionCommand.Refresh();
    }
    public bool IsBusy { get => _busy; private set { Set(ref _busy, value); Changed(nameof(IsIdle)); Changed(nameof(ShowProgressBar)); RefreshCommands(); } }
    public bool IsImporting { get => _importing; private set { if (Set(ref _importing, value)) Changed(nameof(ShowProgressBar)); } }
    public bool ShowProgressBar => IsBusy && !IsImporting;
    public bool IsIdle => !IsBusy;
    public bool CanIntegrateWithWindows => _windowsIntegration.IsAvailable;
    public string LastExportFolder => _settings.LastExportFolder;
    public bool CanOpenLastExportFolder => !string.IsNullOrWhiteSpace(LastExportFolder) && Directory.Exists(LastExportFolder);
    public bool HasImage => _source is not null;
    public bool HasMask => Mask is not null;
    public bool HasGroups => Groups.Count > 0;
    public string FileName => Path.GetFileName(_filePath);
    public Bitmap? Original { get => _original; private set { var old = _original; Set(ref _original, value); old?.Dispose(); } }
    public Bitmap? Mask { get => _mask; private set { var old = _mask; Set(ref _mask, value); old?.Dispose(); Changed(nameof(HasMask)); } }
    public int Progress { get => _progress; private set { Set(ref _progress, value); Changed(nameof(IsIndeterminate)); } }
    public bool IsIndeterminate => Progress == 0;
    public string Status => string.Format(Text[_statusKey], _statusArgs);
    public string ErrorDetails => string.Join(Environment.NewLine, _failures.Select(f => $"{f.FilePath}: {ErrorText(f.Error)}"));
    public bool HasErrors => _failures.Count > 0;
    public ObservableCollection<ImageFileViewModel> Images { get; } = [];
    public ImageFileViewModel? SelectedImage
    {
        get => _selectedImage;
        set
        {
            if (IsBusy || !Set(ref _selectedImage, value)) return;
            _ = RunAsync(LoadSelectedAsync);
        }
    }
    public ObservableCollection<ColorGroupViewModel> Groups { get; } = [];
    public ColorGroupViewModel? SelectedGroup
    {
        get => _selected;
        set
        {
            if (!Set(ref _selected, value)) return;
            RefreshMask();
            DetailPreviewCommand.Refresh();
        }
    }
    public AsyncCommand OpenCommand { get; }
    public AsyncCommand AnalyzeCommand { get; }
    public AsyncCommand ExportCommand { get; }
    public AsyncCommand ExportBatchCommand { get; }
    public AsyncCommand ExportCombinedBatchCommand { get; }
    public AsyncCommand ClearImagesCommand { get; }
    public AsyncCommand RemoveImageCommand { get; }
    public AsyncCommand CancelCommand { get; }
    public AsyncCommand DetailPreviewCommand { get; }
    public AsyncCommand SelectAllColorsCommand { get; }
    public AsyncCommand DeselectAllColorsCommand { get; }
    public AsyncCommand LicensesCommand { get; }
    public AsyncCommand EnableWindowsIntegrationCommand { get; }
    public AsyncCommand RemoveWindowsIntegrationCommand { get; }
    public AsyncCommand OpenLastExportFolderCommand { get; }
    public AsyncCommand ResetScaleCommand { get; }
    public AsyncCommand TripleContourResolutionCommand { get; }

    private void RefreshMask()
    {
        if (_analysis is null || (ShowCombinedPreview ? ExportColorCount == 0 : SelectedGroup is null)) { Mask = null; return; }
        byte[] bytes = ShowCombinedPreview
            ? _service.CombinedPreview(_analysis, Groups.Where(g => g.ExportEnabled).Select(g => g.Group.Index).ToHashSet(), CurrentExportOptions, SelectedImage?.Scale)
            : _service.MaskPreview(_analysis, SelectedGroup!.Group.Index, options: CurrentExportOptions, sizing: SelectedImage?.Scale);
        using var stream = new MemoryStream(bytes);
        Mask = new Bitmap(stream);
    }

    public void RefreshLastExportFolder()
    {
        Changed(nameof(CanOpenLastExportFolder));
        OpenLastExportFolderCommand.Refresh();
    }

    private Task OpenLastExportFolderAsync()
    {
        try
        {
            if (CanOpenLastExportFolder) _exportFolderService.Open(LastExportFolder);
        }
        catch (Exception error) { StatusMessage("Error", error.Message); }
        finally { RefreshLastExportFolder(); }
        return Task.CompletedTask;
    }

    private void RememberExportFolder(string folder)
    {
        _settings.LastExportFolder = folder;
        Changed(nameof(LastExportFolder));
        RefreshLastExportFolder();
        SaveSettings();
    }

    private Task UpdateWindowsIntegrationAsync(bool enable)
    {
        try
        {
            if (enable) _windowsIntegration.Enable(); else _windowsIntegration.Remove();
            WindowsStatusMessage(enable ? "WindowsEnabled" : "WindowsRemoved");
        }
        catch (Exception error) { WindowsStatusMessage("Error", error.Message); }
        return Task.CompletedTask;
    }

    private void WindowsStatusMessage(string key, params object[] args)
    {
        _integrationFeedback = (key, args); Changed(nameof(WindowsIntegrationStatus));
        StatusMessage(key, args);
    }

    private async Task ShowLicensesAsync()
    {
        IsLicenseWindowOpen = true;
        try { await _licenseWindow!.ShowAsync(Text); }
        catch (Exception error) { StatusMessage("Error", error.Message); }
        finally { IsLicenseWindowOpen = false; }
    }

    private async Task OpenAsync()
    {
        IsImporting = true;
        try
        {
            await RunAsync(async token =>
            {
                var paths = await _dialogs.OpenImagesAsync(Text);
                if (paths.Count == 0) return;
                await AddImagesCoreAsync(paths, token);
            });
        }
        finally { IsImporting = false; }
    }

    // Picker, Explorer launch and drop all use the same validation and loading path.
    public async Task AddImagesAsync(IEnumerable<string> paths)
    {
        if (IsBusy) return;
        string[] snapshot = paths.ToArray();
        if (snapshot.Length == 0) return;
        IsImporting = true;
        try { await RunAsync(token => AddImagesCoreAsync(snapshot, token)); }
        finally { IsImporting = false; }
    }

    private async Task AddImagesCoreAsync(IEnumerable<string> paths, CancellationToken token)
    {
        var failures = new List<BatchFailure>();
        ImageFileViewModel? firstAdded = null;
        SetFailures([]);
        foreach (string candidate in paths)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                if (!SupportedImageFiles.IsSupported(candidate)) throw new UnsupportedImageFormatException();
                string path = Path.GetFullPath(candidate);
                if (Images.Any(image => string.Equals(image.FilePath, path, StringComparison.OrdinalIgnoreCase))) continue;
                var preview = await Task.Run(() =>
                {
                    using var image = _service.Load(path);
                    return (Bytes: _service.OriginalPreview(image, 200, 140), image.Width, image.Height);
                }, token);
                token.ThrowIfCancellationRequested();
                using var stream = new MemoryStream(preview.Bytes);
                var item = new ImageFileViewModel(path, new Bitmap(stream), preview.Width, preview.Height, _settings.DefaultExportDpi, _settings.ScaleUnit);
                Images.Add(item);
                Laser.Invalidate();
                firstAdded ??= item;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception error) when (error is not OutOfMemoryException) { failures.Add(new(candidate, error)); }
            SetFailures(failures.ToArray());
        }
        if (firstAdded is not null)
        {
            Set(ref _selectedImage, firstAdded, nameof(SelectedImage));
            await LoadSelectedAsync(token);
        }
        if (failures.Count > 0) StatusMessage("LoadErrors", Images.Count, failures.Count);
        else StatusMessage("ImagesLoaded", Images.Count);
    }

    private async Task LoadSelectedAsync(CancellationToken token)
    {
        ResetPreviewViewports();
        _source?.Dispose(); _source = null; Original = null;
        ClearAnalysis(); Changed(nameof(HasImage));
        _filePath = SelectedImage?.FilePath ?? ""; Changed(nameof(FileName));
        RefreshScaleProperties();
        if (SelectedImage is null) return;
        var loaded = await Task.Run(() => _service.Load(_filePath), token);
        try
        {
            SelectedImage.UpdateSourceSize(loaded.Width, loaded.Height);
            var sizing = SelectedImage.Scale;
            var preview = await Task.Run(() => _service.OriginalPreview(loaded, sizing: sizing), token);
            token.ThrowIfCancellationRequested();
            using var stream = new MemoryStream(preview);
            var bitmap = new Bitmap(stream);
            _source?.Dispose(); _source = loaded; loaded = null!;
            Original = bitmap;
            ClearAnalysis(); Changed(nameof(HasImage)); Changed(nameof(FileName));
            RefreshScaleProperties();
            StatusMessage("Loaded");
            if (SelectedImage.ColorsDetected)
            {
                var result = await Task.Run(() => _service.Analyze(_source!, Tolerance, token, (byte)MinimumAlpha), token);
                token.ThrowIfCancellationRequested(); ApplyAnalysis(result);
                StatusMessage("Found", Groups.Count, result.Width, result.Height);
            }
        }
        finally { loaded?.Dispose(); }
    }

    private Task ClearImagesAsync()
    {
        Set(ref _selectedImage, null, nameof(SelectedImage));
        var oldImages = Images.ToArray();
        Images.Clear();
        Laser.ClearLayout();
        foreach (var image in oldImages) image.Dispose();
        _source?.Dispose(); _source = null; Original = null;
        _filePath = ""; ClearAnalysis(); SetFailures([]);
        Changed(nameof(HasImage)); Changed(nameof(FileName)); RefreshCommands(); StatusMessage("Ready");
        RefreshScaleProperties();
        return Task.CompletedTask;
    }

    private async Task RemoveImageAsync() => await RunAsync(async token =>
    {
        var removed = SelectedImage!;
        int index = Images.IndexOf(removed);
        Images.Remove(removed);
        Laser.Invalidate();
        Set(ref _selectedImage, Images.Count == 0 ? null : Images[Math.Min(index, Images.Count - 1)], nameof(SelectedImage));
        removed.Dispose();
        await LoadSelectedAsync(token);
        StatusMessage(Images.Count == 0 ? "Ready" : "ImagesLoaded", Images.Count);
    });

    private async Task AnalyzeAsync() => await RunAsync(async token =>
    {
        Laser.Invalidate();
        var failures = new List<BatchFailure>(); SetFailures([]);
        int completed = 0;
        for (int i = 0; i < Images.Count; i++)
        {
            token.ThrowIfCancellationRequested();
            var image = Images[i];
            StatusMessage("AnalyzeWorking", i + 1, Images.Count, image.FileName);
            try
            {
                IReadOnlyList<ColorGroup> palette;
                if (image == SelectedImage && _source is not null)
                {
                    var result = await Task.Run(() => _service.Analyze(_source, Tolerance, token, (byte)MinimumAlpha), token);
                    token.ThrowIfCancellationRequested();
                    image.ColorsDetected = true; ApplyAnalysis(result); palette = result.Groups;
                }
                else
                {
                    // Retain only each image's palette/selection, never full-resolution label grids for all files.
                    palette = await Task.Run(() =>
                    {
                        using var source = _service.Load(image.FilePath);
                        return _service.Analyze(source, Tolerance, token, (byte)MinimumAlpha).Groups;
                    }, token);
                    token.ThrowIfCancellationRequested();
                    image.ExportColors = palette.Where(g => image.ExportColors?.Contains(g.Hex) ?? true).Select(g => g.Hex).ToHashSet();
                    image.ColorsDetected = true;
                }
                completed++;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                image.ColorsDetected = false; image.ExportColors = new HashSet<string>();
                if (image == SelectedImage) ClearAnalysis();
                failures.Add(new(image.FilePath, error)); SetFailures(failures.ToArray());
            }
            Progress = (i + 1) * 100 / Images.Count;
        }
        if (_source is null && SelectedImage?.ColorsDetected == true) await LoadSelectedAsync(token);
        if (Images.Count == 1 && failures.Count == 0 && _analysis is not null)
            StatusMessage("Found", Groups.Count, _analysis.Width, _analysis.Height);
        else StatusMessage("ImagesAnalyzed", completed, Images.Count, failures.Count);
    });

    private void ApplyAnalysis(ColorAnalysis result)
    {
        _analysis = result;
        foreach (var old in Groups) old.PropertyChanged -= ExportSelectionChanged;
        Groups.Clear();
        foreach (var group in result.Groups)
        {
            var item = new ColorGroupViewModel(group, (long)result.Width * result.Height)
            { ExportEnabled = SelectedImage?.ExportColors?.Contains(group.Hex) ?? true };
            item.PropertyChanged += ExportSelectionChanged;
            Groups.Add(item);
        }
        ExportSelectionChanged(null, new(nameof(ColorGroupViewModel.ExportEnabled)));
        Changed(nameof(HasGroups)); SelectedGroup = Groups.FirstOrDefault();
    }

    private async Task ExportAsync() => await RunAsync(async token =>
    {
        string? parent = await _dialogs.SelectOutputAsync(Text);
        if (parent is null) return;
        var result = _analysis!;
        var selected = Groups.Where(g => g.ExportEnabled).Select(g => g.Group.Index).ToHashSet();
        var options = CurrentExportOptions;
        var sizing = SelectedImage!.Scale;
        var progress = new Progress<int>(value => Progress = value);
        string folder = await Task.Run(() => _service.Export(result, parent, _filePath, progress, token, selected, options, sizing), token);
        StatusMessage("Exported", selected.Count, folder);
        RememberExportFolder(folder);
    });

    private Task ExportBatchAsync() => ExportBatchAsync(false);

    private async Task ExportBatchAsync(bool combineGroups) => await RunAsync(async token =>
    {
        string? parent = await _dialogs.SelectOutputAsync(Text);
        if (parent is null) return;
        SetFailures([]);
        string[] paths = Images.Select(image => image.FilePath).ToArray();
        double tolerance = Tolerance;
        byte alpha = (byte)MinimumAlpha;
        var options = CurrentExportOptions;
        var imageScales = Images.ToDictionary(image => image.FilePath, image => image.Scale, StringComparer.OrdinalIgnoreCase);
        var selections = Images.ToDictionary(image => image.FilePath,
            image => (IReadOnlySet<string>)(image.ColorsDetected ? image.ExportColors ?? new HashSet<string>() : new HashSet<string>()).ToHashSet(),
            StringComparer.OrdinalIgnoreCase);
        var progress = new Progress<BatchProgress>(value =>
        {
            Progress = value.Percent;
            StatusMessage("BatchWorking", value.FileNumber, value.TotalFiles, value.FileName);
        });
        var result = await Task.Run(() => new BatchExportService().Export(paths, parent, tolerance, progress, token, alpha, selections, options, imageScales, combineGroups), token);
        SetFailures(result.Failures);
        StatusMessage(result.Cancelled ? "BatchCancelled" : "BatchDone", result.ExportedImages,
            result.ExportedMasks, result.Failures.Count, result.Folder, result.SkippedImages);
        if (result.ExportedImages > 0) RememberExportFolder(result.Folder);
    });

    private async Task ShowDetailPreviewAsync() => await RunAsync(async token =>
    {
        var analysis = _analysis!;
        bool combined = ShowCombinedPreview;
        int group = SelectedGroup?.Group.Index ?? -1;
        var selected = Groups.Where(g => g.ExportEnabled).Select(g => g.Group.Index).ToHashSet();
        var options = CurrentExportOptions;
        var sizing = SelectedImage!.Scale;
        var size = sizing.GetSize(analysis.Width, analysis.Height);
        var bytes = await Task.Run(() =>
        {
            byte[] original = _service.OriginalPreview(_source!, size.Width, size.Height, sizing);
            token.ThrowIfCancellationRequested();
            byte[] mask = combined ? _service.CombinedPreview(analysis, selected, options, sizing, fullResolution: true, token: token)
                : _service.MaskPreview(analysis, group, fullResolution: true, options: options, sizing: sizing, token: token);
            byte[] overlay = combined ? _service.OverlayPreview(analysis, selected, token, sizing, options.SmoothContours)
                : _service.OverlayPreview(analysis, group, token, sizing, options.SmoothContours);
            return (original, mask, overlay);
        }, token);
        token.ThrowIfCancellationRequested();
        _detailPreview!.Show(new(bytes.original, bytes.mask, bytes.overlay, Text, $"{FileName} · {(combined ? Text["CombinedPreview"] : SelectedGroup!.Hex)}"));
    });

    private Task SelectColors(bool enabled)
    {
        _updatingColorSelection = true;
        try { foreach (var group in Groups) group.ExportEnabled = enabled; }
        finally { _updatingColorSelection = false; }
        ExportSelectionChanged(null, new(nameof(ColorGroupViewModel.ExportEnabled)));
        return Task.CompletedTask;
    }

    private void ExportSelectionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(ColorGroupViewModel.ExportEnabled)) return;
        if (_updatingColorSelection) return;
        if (SelectedImage is not null)
        {
            var colors = Groups.Where(g => g.ExportEnabled).Select(g => g.Hex).ToHashSet();
            if (SelectedImage.ExportColors is null || !SelectedImage.ExportColors.SetEquals(colors)) Laser.Invalidate();
            SelectedImage.ExportColors = colors;
        }
        Changed(nameof(ExportColorCount)); Changed(nameof(HasBatchSelection)); ExportCommand.Refresh(); ExportBatchCommand.Refresh(); ExportCombinedBatchCommand.Refresh();
        if (RightSidebarCollapsed || IsLaserMode) _colorsNotice = true; RefreshWorkspaceLabels();
        if (ShowCombinedPreview) RefreshMask();
        DetailPreviewCommand.Refresh();
        Laser.RefreshCommands();
    }

    private void InvalidateSelections()
    {
        Laser.Invalidate();
        foreach (var image in Images) { image.ExportColors = null; image.ColorsDetected = false; }
        ClearAnalysis();
    }

    private async Task RunAsync(Func<CancellationToken, Task> action)
    {
        IsBusy = true; Progress = 0;
        using var cancellation = new CancellationTokenSource(); _cancellation = cancellation;
        string previousKey = _statusKey; object[] previousArgs = _statusArgs;
        StatusMessage("Working");
        try
        {
            await action(cancellation.Token);
            if (_statusKey == "Working") StatusMessage(previousKey, previousArgs);
        }
        catch (OperationCanceledException) { StatusMessage("Cancelled"); }
        catch (ImageLimitException) { StatusMessage("TooLarge"); }
        catch (ExportSizeLimitException) { StatusMessage("ExportTooLarge"); }
        catch (ColorLimitException) { StatusMessage("TooMany"); }
        catch (EmptyImageException) { StatusMessage("Empty"); }
        catch (NoColorsSelectedException) { StatusMessage("NoColors"); }
        catch (LaserSettingsException) { StatusMessage("LaserInvalidSettings"); }
        catch (LaserNoPartsException) { StatusMessage("LaserNoParts"); }
        catch (LaserComplexityException) { StatusMessage("LaserTooComplex"); }
        catch (LaserPartTooLargeException error) { StatusMessage("LaserPartTooLarge", error.SourceName, error.WidthCm, error.HeightCm); }
        catch (Exception e) { StatusMessage("Error", e.Message); }
        finally { _cancellation = null; IsBusy = false; }
    }

    private void ClearAnalysis()
    {
        _analysis = null; SelectedGroup = null; Mask = null;
        foreach (var group in Groups) group.PropertyChanged -= ExportSelectionChanged;
        Groups.Clear(); Changed(nameof(HasGroups)); Changed(nameof(ExportColorCount)); RefreshCommands();
    }
    private void StatusMessage(string key, params object[] args) { _statusKey = key; _statusArgs = args; Changed(nameof(Status)); }
    private string ErrorText(Exception error) => error switch
    {
        ImageLimitException => Text["TooLarge"], ColorLimitException => Text["TooMany"],
        EmptyImageException => Text["Empty"], NoColorsSelectedException => Text["NoColors"],
        UnsupportedImageFormatException => Text["UnsupportedImage"], ExportSizeLimitException => Text["ExportTooLarge"], _ => error.Message
    };
    private void SetFailures(IReadOnlyList<BatchFailure> failures)
    {
        _failures = failures; Changed(nameof(ErrorDetails)); Changed(nameof(HasErrors));
    }
    private void RefreshCommands()
    {
        OpenCommand.Refresh(); AnalyzeCommand.Refresh(); ExportCommand.Refresh(); CancelCommand.Refresh();
        ExportBatchCommand.Refresh(); ClearImagesCommand.Refresh(); RemoveImageCommand.Refresh();
        ExportCombinedBatchCommand.Refresh();
        DetailPreviewCommand.Refresh(); SelectAllColorsCommand.Refresh(); DeselectAllColorsCommand.Refresh();
        EnableWindowsIntegrationCommand.Refresh(); RemoveWindowsIntegrationCommand.Refresh();
        ResetScaleCommand.Refresh();
        TripleContourResolutionCommand.Refresh();
        Laser.RefreshCommands();
    }
    private void SaveSettings() { if (!_settingsService.Save(_settings)) StatusMessage("SaveError"); }
    public void Cancel() => _cancellation?.Cancel();
    public void Dispose()
    {
        Cancel(); DisposeWorkspacePreferences(); SaveSettings();
        Laser.Dispose();
        Set(ref _selectedImage, null, nameof(SelectedImage));
        _source?.Dispose(); _source = null; Original = null; Mask = null;
        foreach (var image in Images) image.Dispose();
        Images.Clear();
    }
}
